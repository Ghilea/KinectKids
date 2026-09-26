[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $projectRoot 'src\KinectBridge\KinectBridge.csproj'
$output = Join-Path $projectRoot "src\KinectBridge\bin\$Configuration\KinectBridge.exe"

$sdkPaths = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll'),
    (Join-Path $env:ProgramFiles 'Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll')
) | Where-Object { $_ }
if (-not ($sdkPaths | Where-Object { Test-Path $_ })) {
    throw 'Kinect for Windows SDK 1.8 saknas.'
}

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) {
    throw 'Visual Studio 2022 med .NET desktop development saknas.'
}
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild hittades inte.' }

Write-Host 'Bygger Kinect-bryggan som 32-bitarsprogram…' -ForegroundColor Cyan
& $msbuild $project /restore /m /p:Configuration=$Configuration /p:Platform=x86 /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw "Kinect-bryggan kunde inte byggas (felkod $LASTEXITCODE)." }
if (-not (Test-Path $output)) { throw "Programfilen hittades inte: $output" }
$packaged = Join-Path $projectRoot 'unity\KinectKids3D\Assets\StreamingAssets\KinectBridge\KinectBridge.exe'
$packagedDirectory = Split-Path -Parent $packaged
if (-not (Test-Path $packagedDirectory)) { New-Item -ItemType Directory -Path $packagedDirectory -Force | Out-Null }
Copy-Item -LiteralPath $output -Destination $packaged -Force
Write-Host "Kinect-bryggan är klar: $output" -ForegroundColor Green
