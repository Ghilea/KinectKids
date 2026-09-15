[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $PSScriptRoot 'Build.ps1'
$executable = Join-Path $projectRoot 'src\KinectKids\bin\Release\KinectKids.exe'

# Kör byggskriptet utan en pipeline så att riktiga MSBuild-fel syns i konsolen.
& $buildScript -Configuration Release
if (-not (Test-Path $executable)) { throw "Programfilen hittades inte: $executable" }

Write-Host 'Startar Rörelselek…' -ForegroundColor Cyan
Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable)
