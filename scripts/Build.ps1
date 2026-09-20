[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$unityProject = Join-Path $projectRoot 'unity\KinectKids3D'
$unityEditor = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe'
$output = Join-Path $unityProject 'Build\KinectKids\KinectKids.exe'
$log = Join-Path $unityProject 'Build\kinectkids-platform-build.log'

if (-not (Test-Path -LiteralPath $unityEditor)) {
    throw "Unity 6000.0.60f1 hittades inte: $unityEditor"
}

$arguments = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"' + $unityProject + '"'),
    '-executeMethod', 'KinectKids3D.Editor.KinectKidsPlatformBuilder.BuildPlatform',
    '-logFile', ('"' + $log + '"')
)

Write-Host 'Bygger den gemensamma Unity-plattformen KinectKids...' -ForegroundColor Cyan
$process = Start-Process -FilePath $unityEditor -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
if ($process.ExitCode -ne 0) {
    throw "Unity-bygget misslyckades med felkod $($process.ExitCode). Se $log"
}
if (-not (Test-Path -LiteralPath $output)) {
    throw "Unity rapporterade klart men programfilen saknas: $output"
}

Write-Host "Klart: $output" -ForegroundColor Green
Write-Host 'Detta ar den enda version som ska startas av slutkunden.' -ForegroundColor Green
