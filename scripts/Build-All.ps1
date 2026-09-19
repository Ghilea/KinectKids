[CmdletBinding()]
param(
    [switch]$SkipUnity
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$unityProject = Join-Path $projectRoot 'unity\KinectKids3D'
$unityBuild = Join-Path $unityProject 'Build\Spokjakten3D\Spokjakten3D.exe'
$greveGastBuild = Join-Path $unityProject 'Build\GreveGastJakt\GreveGastJakt.exe'
$launcher = Join-Path $projectRoot 'src\KinectKids\bin\Release\KinectKids.exe'
$unityEditor = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe'

Write-Host 'Bygger den gemensamma KinectKids-menyn…' -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Build.ps1') -Configuration Release

if (-not $SkipUnity) {
    if (-not (Test-Path -LiteralPath $unityEditor)) {
        throw "Unity 6000.0.60f1 hittades inte: $unityEditor"
    }

    Write-Host 'Bygger fristående Spökjakten…' -ForegroundColor Cyan
    $log = Join-Path $unityProject 'Build\unity-build.log'
    $unityArguments = @(
        '-batchmode',
        '-nographics',
        '-quit',
        '-projectPath', ('"' + $unityProject + '"'),
        '-executeMethod', 'KinectKids3D.Editor.KinectKidsWindowsBuild.BuildSchoolVersion',
        '-logFile', ('"' + $log + '"')
    )
    $unityProcess = Start-Process -FilePath $unityEditor -ArgumentList $unityArguments `
        -Wait -PassThru -WindowStyle Hidden
    if ($unityProcess.ExitCode -ne 0) {
        throw "Unity-bygget misslyckades med felkod $($unityProcess.ExitCode). Se $log"
    }

    Write-Host 'Bygger fristaende Greve Gasts Jakt...' -ForegroundColor Cyan
    $greveLog = Join-Path $unityProject 'Build\greve-gast-build.log'
    $greveArguments = @(
        '-batchmode',
        '-nographics',
        '-quit',
        '-projectPath', ('"' + $unityProject + '"'),
        '-executeMethod', 'KinectKids3D.Editor.KinectKidsWindowsBuild.BuildGreveGastVersion',
        '-logFile', ('"' + $greveLog + '"')
    )
    $greveProcess = Start-Process -FilePath $unityEditor -ArgumentList $greveArguments `
        -Wait -PassThru -WindowStyle Hidden
    if ($greveProcess.ExitCode -ne 0) {
        throw "Greve Gast-bygget misslyckades med felkod $($greveProcess.ExitCode). Se $greveLog"
    }
}

if (-not (Test-Path -LiteralPath $launcher)) {
    throw "Huvudmenyn saknas efter bygget: $launcher"
}
if (-not (Test-Path -LiteralPath $unityBuild)) {
    throw "Spökjakten saknas efter bygget: $unityBuild"
}
if (-not (Test-Path -LiteralPath $greveGastBuild)) {
    throw "Greve Gasts Jakt saknas efter bygget: $greveGastBuild"
}

Write-Host ''
Write-Host 'HELA SPELET ÄR KLART' -ForegroundColor Green
Write-Host "Starta alltid: $launcher" -ForegroundColor Green
Write-Host 'Unity Hub behövs inte för att spela.' -ForegroundColor Green
