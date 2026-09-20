[CmdletBinding()]
param([switch]$Rebuild)

$ErrorActionPreference = 'Stop'
$executable = Join-Path $env:LOCALAPPDATA 'KinectKids\Build\KinectKids\KinectKids.exe'

if ($Rebuild -or -not (Test-Path -LiteralPath $executable)) {
    & (Join-Path $PSScriptRoot 'Build.ps1')
}
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Unity-programmet hittades inte: $executable"
}

Write-Host 'Startar KinectKids Unity-plattform...' -ForegroundColor Cyan
Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable)
