# Deploys the checked-out branch for a UAT (Windows, PowerShell 7): stops the running extension,
# Command Palette and PowerToys, builds the extension through build.cs, registers its package, restarts PowerToys.
# Run from anywhere: .\uat.ps1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Stop-Running([string]$Name) {
    $processes = @(Get-Process -Name $Name -ErrorAction SilentlyContinue)
    if ($processes.Count -eq 0) {
        Write-Host "$Name was not running"
        return $null
    }
    $processes | Stop-Process
    Write-Host "Stopped $Name (pid $($processes.Id -join ', '))"
    return $processes[0].Path
}

Stop-Running CmdPal.Extension.Shortcuts | Out-Null
# Command Palette runs on its own; PowerToys ends it only when the module is disabled, and a
# Command Palette that outlives a registration lists the extension once more.
Stop-Running Microsoft.CmdPal.UI | Out-Null
$powerToysPath = Stop-Running PowerToys

dotnet run "$PSScriptRoot/build.cs" -- extension
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$manifests = @(Get-ChildItem "$PSScriptRoot/src/CmdPal.Extension.Shortcuts/bin/x64/Debug" -Recurse -Filter AppxManifest.xml)
if ($manifests.Count -ne 1) {
    throw "Expected one AppxManifest.xml under src/CmdPal.Extension.Shortcuts/bin/x64/Debug, found $($manifests.Count)."
}
Add-AppxPackage -Register $manifests[0].FullName
Write-Host "Registered $($manifests[0].FullName)"

$candidates = @($powerToysPath, "$env:USERPROFILE\scoop\apps\powertoys\current\PowerToys.exe")
$exe = $candidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if ($exe) {
    Start-Process $exe
    Write-Host "Started $exe"
} else {
    Write-Host "PowerToys.exe not found; looked in: $($candidates -join ', ')"
}
