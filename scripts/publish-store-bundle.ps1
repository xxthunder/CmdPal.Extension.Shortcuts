# Attaches the Store-signed bundle to the latest GitHub release and updates the scoop and WinGet
# manifests (Windows, PowerShell 7, gh, komac). Run by hand once the Store has certified the release.
# Based on scripts/publish-msstore-msix.ps1 of 8LWXpg/CmdPal-ProcessKiller (MIT, see
# THIRD-PARTY-NOTICES.md); this copy adds the check of signer and version before the upload.
# Usage: ./scripts/publish-store-bundle.ps1 [-WhatIf]
#Requires -Version 7
[CmdletBinding(SupportsShouldProcess)]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ProductId = '9NN0F5R302LZ'
$PackageIdentifier = 'karnomag.ShortcutsforCommandPalette'
$Repo = 'xxthunder/CmdPal.Extension.Shortcuts'

$tag = gh release view --repo $Repo --json tagName --jq .tagName
if ($LASTEXITCODE -ne 0 -or -not $tag) {
    throw 'Could not read the latest GitHub release.'
}
$version = $tag.TrimStart('v')
$fileName = "${PackageIdentifier}_$version.msixbundle"

$headers = @{
    'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36'
    'Origin'     = 'https://store.rg-adguard.net'
    'Referer'    = 'https://store.rg-adguard.net/'
}
$body = @{ type = 'ProductId'; url = $ProductId; ring = 'RP' }
$response = Invoke-WebRequest -Uri 'https://store.rg-adguard.net/api/GetFiles' -Method Post -Headers $headers -Body $body

if (-not ($link = $response.Links | Where-Object outerHTML -Like '*.msixbundle</a>' | Select-Object -Last 1)) {
    throw 'No msixbundle link found in store.rg-adguard.net response.'
}
# The href keeps HTML entities such as &amp;; the download server answers those with 403.
$url = [System.Net.WebUtility]::HtmlDecode($link.href)

$outFile = Join-Path ([System.IO.Path]::GetTempPath()) $fileName
Write-Host "Downloading $fileName ..."
Invoke-WebRequest -Uri $url -OutFile $outFile

$signature = Get-AuthenticodeSignature $outFile
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Issuer -notlike 'CN=Microsoft Marketplace CA*') {
    throw "$fileName is not signed by the Store: status $($signature.Status), issuer '$($signature.SignerCertificate.Issuer)'."
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($outFile)
try {
    $entry = $zip.GetEntry('AppxMetadata/AppxBundleManifest.xml')
    if (-not $entry) { throw "$fileName has no AppxMetadata/AppxBundleManifest.xml." }
    $reader = [System.IO.StreamReader]::new($entry.Open())
    try { $identity = ([xml]$reader.ReadToEnd()).Bundle.Identity } finally { $reader.Dispose() }
} finally {
    $zip.Dispose()
}
if ($identity.Name -ne $PackageIdentifier -or [version]$identity.Version -ne [version]"$version.0") {
    throw "The Store has $($identity.Name) $($identity.Version), the release is $tag. Try again once the Store has certified $tag."
}
Write-Host "Checked: $($identity.Name) $($identity.Version), signed by $($signature.SignerCertificate.Issuer)"

if ($PSCmdlet.ShouldProcess($tag, "Upload $fileName")) {
    gh release upload --repo $Repo $tag $outFile --clobber
    if ($LASTEXITCODE -ne 0) {
        throw 'gh release upload failed.'
    }
}

$bucketManifest = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..' 'bucket' 'cmdpal-shortcuts.json'))
if ($PSCmdlet.ShouldProcess($bucketManifest, "Set version $version")) {
    $scoop = Get-Content -Raw $bucketManifest | ConvertFrom-Json
    $scoop.version = $version
    $scoop.url = "https://github.com/$Repo/releases/download/$tag/$fileName"
    $scoop.hash = (Get-FileHash $outFile -Algorithm SHA256).Hash.ToLowerInvariant()
    # LF and a final newline, as the file is committed.
    [System.IO.File]::WriteAllText($bucketManifest, (($scoop | ConvertTo-Json -Depth 5) -replace "`r`n", "`n") + "`n")
    Write-Host "Updated $bucketManifest; commit and push it so that scoop users get $version."
}

if ($PSCmdlet.ShouldProcess($PackageIdentifier, "komac update $version")) {
    if (-not ($assetUrl = (gh release view --repo $Repo $tag --json assets | ConvertFrom-Json).assets |
                Where-Object name -EQ $fileName |
                Select-Object -ExpandProperty url)) {
        throw 'Could not resolve uploaded asset URL.'
    }
    # komac update changes an existing package only; the first manifest comes from komac new.
    $manifestPath = 'manifests/k/' + $PackageIdentifier.Replace('.', '/')
    gh api "repos/microsoft/winget-pkgs/contents/$manifestPath" --silent 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "microsoft/winget-pkgs has no $PackageIdentifier yet. Make the first manifest once: komac new $PackageIdentifier --version $version --urls $assetUrl"
    }
    # komac opens its pull request from the user's fork and does not create one.
    $login = gh api user --jq .login
    $fork = gh api "repos/$login/winget-pkgs" --jq '.fork and .parent.full_name == "microsoft/winget-pkgs"' 2>$null
    if ($LASTEXITCODE -ne 0 -or $fork -ne 'true') {
        throw "$login has no fork of microsoft/winget-pkgs. Make it once: gh repo fork microsoft/winget-pkgs --clone=false"
    }
    # komac does not use gh's login; hand it gh's token unless one is set.
    if (-not $env:GITHUB_TOKEN) {
        $env:GITHUB_TOKEN = gh auth token
    }
    Write-Host "Updating winget manifest for $PackageIdentifier $version ..."
    komac update $PackageIdentifier --version $version --urls $assetUrl
    if ($LASTEXITCODE -ne 0) {
        throw 'komac update failed.'
    }
}
