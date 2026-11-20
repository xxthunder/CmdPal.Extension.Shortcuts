# Attaches the Store-signed bundle to the latest GitHub release and updates the WinGet manifest
# (Windows, PowerShell 7, gh, komac). Run by hand once the Store has certified the release.
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

if ($PSCmdlet.ShouldProcess($PackageIdentifier, "komac update $version")) {
    if (-not ($assetUrl = (gh release view --repo $Repo $tag --json assets | ConvertFrom-Json).assets |
                Where-Object name -EQ $fileName |
                Select-Object -ExpandProperty url)) {
        throw 'Could not resolve uploaded asset URL.'
    }
    Write-Host "Updating winget manifest for $PackageIdentifier $version ..."
    komac update $PackageIdentifier --version $version --urls $assetUrl
    if ($LASTEXITCODE -ne 0) {
        throw 'komac update failed.'
    }
}
