# Deploys the checked-out branch for a UAT (Windows, PowerShell 7): stops the running extension
# and PowerToys, builds the extension through build.cs, registers its package, restarts PowerToys.
# Run from anywhere: .\uat.ps1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$powerToysPath = Get-Process PowerToys -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Path
Stop-Process -Name CmdPal.Extension.Shortcuts -ErrorAction SilentlyContinue
Stop-Process -Name PowerToys -ErrorAction SilentlyContinue

dotnet run "$PSScriptRoot/build.cs" -- extension
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$manifests = @(Get-ChildItem "$PSScriptRoot/src/CmdPal.Extension.Shortcuts/bin/x64/Debug" -Recurse -Filter AppxManifest.xml)
if ($manifests.Count -ne 1) {
    throw "Expected one AppxManifest.xml under src/CmdPal.Extension.Shortcuts/bin/x64/Debug, found $($manifests.Count)."
}
Add-AppxPackage -Register $manifests[0].FullName

$candidates = @($powerToysPath, "$env:LOCALAPPDATA\PowerToys\PowerToys.exe", "$env:ProgramFiles\PowerToys\PowerToys.exe")
$exe = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if ($exe) {
    Start-Process $exe
} else {
    Write-Host "PowerToys.exe not found; looked in: $($candidates -join ', ')"
}
