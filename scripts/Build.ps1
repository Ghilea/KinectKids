[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$unityProject = Join-Path $projectRoot 'unity\KinectKids3D'
$unityEditor = Join-Path ${env:ProgramFiles} 'Unity\Hub\Editor\6000.0.60f1\Editor\Unity.exe'
$output = Join-Path $env:LOCALAPPDATA 'KinectKids\Build\KinectKids\KinectKids.exe'
$log = Join-Path $unityProject 'Build\kinectkids-platform-build.log'
$verifyLog = Join-Path $env:LOCALAPPDATA 'KinectKids\Build\kinectkids-build-verification.log'

if (-not (Test-Path -LiteralPath $unityEditor)) {
    throw "Unity 6000.0.60f1 hittades inte: $unityEditor"
}

$normalizedProject = $unityProject.Replace('\', '/').ToLowerInvariant()
$openEditor = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -and
        $_.CommandLine.Replace('\', '/').ToLowerInvariant().Contains($normalizedProject) -and
        $_.CommandLine -notmatch 'AssetImportWorker'
    } |
    Select-Object -First 1
if ($openEditor) {
    throw 'Unity-projektet ar redan oppet. Stang Unity-editorn och kor byggkommandot igen.'
}

$arguments = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"' + $unityProject + '"'),
    '-executeMethod', 'KinectKids3D.Editor.KinectKidsPlatformBuilder.BuildPlatform',
    '-logFile', ('"' + $log + '"')
)

$verified = $false
for ($attempt = 1; $attempt -le 3 -and -not $verified; $attempt++) {
    Write-Host "Bygger den gemensamma Unity-plattformen KinectKids (forsok $attempt av 3)..." -ForegroundColor Cyan
    $process = Start-Process -FilePath $unityEditor -ArgumentList $arguments -Wait -PassThru -WindowStyle Hidden
    if ($process.ExitCode -ne 0) {
        if ($attempt -eq 3) {
            throw "Unity-bygget misslyckades med felkod $($process.ExitCode). Se $log"
        }
        Write-Warning "Unity-bygget misslyckades. Forsoker igen med en ny byggmapp."
        continue
    }
    if (-not (Test-Path -LiteralPath $output)) {
        if ($attempt -eq 3) { throw "Unity rapporterade klart men programfilen saknas: $output" }
        continue
    }

    if (Test-Path -LiteralPath $verifyLog) { Remove-Item -LiteralPath $verifyLog -Force }
    $verifyArguments = @(
        '-force-d3d11',
        '--platform-smoke-test',
        '-logFile', ('"' + $verifyLog + '"')
    )
    $verification = Start-Process -FilePath $output -ArgumentList $verifyArguments -PassThru -WindowStyle Hidden
    $timedOut = -not $verification.WaitForExit(90000)
    if ($timedOut) {
        Stop-Process -Id $verification.Id -Force
        $verification.WaitForExit()
    }

    $verificationText = if (Test-Path -LiteralPath $verifyLog) {
        Get-Content -LiteralPath $verifyLog -Raw
    } else { '' }
    $hasAllScenes = $verificationText.Contains('PLATFORM_SMOKE: all scenes passed')
    $hasFailure = $verificationText -match 'corrupted|Crash!!!|Exception|MissingReference|NullReference|PLATFORM_SMOKE: .*missing|PLATFORM_SMOKE: timeout'
    $verified = -not $timedOut -and $verification.ExitCode -eq 0 -and $hasAllScenes -and -not $hasFailure
    if (-not $verified) {
        Write-Warning "Speltestet misslyckades (exitkod $($verification.ExitCode)). Se $verifyLog. Bygger om paketet."
    }
}

if (-not $verified) {
    throw "KinectKids kunde inte verifieras efter tre rena byggforsok. Se $verifyLog"
}

Write-Host "Klart och verifierat: $output" -ForegroundColor Green
Write-Host 'Detta ar den enda version som ska startas av slutkunden.' -ForegroundColor Green
