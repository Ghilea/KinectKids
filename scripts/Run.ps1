[CmdletBinding()]
param([switch]$Rebuild)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $projectRoot 'unity\KinectKids3D\Build\KinectKids\KinectKids.exe'

if ($Rebuild -or -not (Test-Path -LiteralPath $executable)) {
    & (Join-Path $PSScriptRoot 'Build.ps1')
}
if (-not (Test-Path -LiteralPath $executable)) {
    throw "Unity-programmet hittades inte: $executable"
}

Write-Host 'Startar KinectKids Unity-plattform...' -ForegroundColor Cyan
Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable)
