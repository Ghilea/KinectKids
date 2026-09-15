[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$executable = & (Join-Path $PSScriptRoot 'Build.ps1') -Configuration Release | Select-Object -Last 1
if (-not (Test-Path $executable)) { throw "Programfilen hittades inte: $executable" }

Write-Host 'Startar Rörelselek…' -ForegroundColor Cyan
Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable)
