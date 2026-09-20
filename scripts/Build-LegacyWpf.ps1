[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $projectRoot 'KinectKids.sln'

Write-Warning 'Detta bygger endast den arkiverade WPF-referensen. Slutkunden ska använda Unity-versionen.'
$sdkPaths = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll'),
    (Join-Path $env:ProgramFiles 'Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll')
) | Where-Object { $_ }
if (-not ($sdkPaths | Where-Object { Test-Path $_ })) { throw 'Kinect for Windows SDK 1.8 saknas.' }

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (-not (Test-Path $vswhere)) { throw 'Visual Studio Installer/vswhere hittades inte.' }
$msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $msbuild) { throw 'MSBuild hittades inte.' }

& $msbuild $solution /restore /m /p:Configuration=$Configuration /p:Platform=x86 /verbosity:minimal
if ($LASTEXITCODE -ne 0) { throw "Legacy-bygget misslyckades med felkod $LASTEXITCODE." }
Write-Host 'Legacy WPF byggd för referenstest. Starta inte denna som KinectKids huvudprogram.' -ForegroundColor Yellow
