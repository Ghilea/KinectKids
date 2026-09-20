[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Namnet behalls for gamla arbetsfloden, men det finns numera bara ett
# slutkundsbygge: den gemensamma Unity-plattformen.
& (Join-Path $PSScriptRoot 'Build.ps1')

Write-Host ''
Write-Host 'HELA UNITY-PLATTFORMEN AR KLAR' -ForegroundColor Green
Write-Host 'Starta alltid STARTA-KINECTKIDS.cmd eller unity\KinectKids3D\Build\KinectKids\KinectKids.exe.' -ForegroundColor Green
