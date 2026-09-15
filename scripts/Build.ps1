[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $projectRoot 'KinectKids.sln'

$sdkPaths = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll'),
    (Join-Path $env:ProgramFiles 'Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll')
) | Where-Object { $_ }

if (-not ($sdkPaths | Where-Object { Test-Path $_ })) {
    throw 'Kinect for Windows SDK 1.8 saknas. Installera SDK 1.8 innan du bygger.'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw 'Visual Studio Installer/vswhere hittades inte. Installera Visual Studio 2022 med .NET desktop development.'
}

$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild hittades inte i Visual Studio-installationen.' }

Write-Host "Bygger $Configuration | x86…" -ForegroundColor Cyan
& $msbuild $solution /restore /m /p:Configuration=$Configuration /p:Platform=x86 /verbosity:minimal
if ($LASTEXITCODE -ne 0) {
    throw "Bygget misslyckades med MSBuild-felkod $LASTEXITCODE. Se felet ovan."
}

$output = Join-Path $projectRoot "src\KinectKids\bin\$Configuration\KinectKids.exe"
if (-not (Test-Path $output)) { throw "Bygget lyckades men programfilen hittades inte: $output" }
Write-Host "Klart: $output" -ForegroundColor Green
