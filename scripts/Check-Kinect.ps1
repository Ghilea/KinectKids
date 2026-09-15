[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

Write-Host 'Rörelselek – Kinect-kontroll' -ForegroundColor Cyan
Write-Host '=============================' -ForegroundColor DarkCyan

$sdkCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll'),
    (Join-Path $env:ProgramFiles 'Microsoft SDKs\Kinect\v1.8\Assemblies\Microsoft.Kinect.dll')
) | Where-Object { $_ }

$sdk = $sdkCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($sdk) {
    Write-Host "[OK] Kinect SDK 1.8: $sdk" -ForegroundColor Green
} else {
    Write-Host '[FEL] Kinect SDK 1.8 hittades inte.' -ForegroundColor Red
    Write-Host 'Installera KinectSDK-v1.8-Setup.exe och kör kontrollen igen.'
}

try {
    $devices = Get-PnpDevice -PresentOnly -ErrorAction Stop | Where-Object {
        $_.InstanceId -like '*VID_045E&PID_02AE*' -or
        $_.FriendlyName -like '*Xbox NUI*' -or
        $_.FriendlyName -like '*Kinect*'
    }
} catch {
    $devices = Get-CimInstance Win32_PnPEntity | Where-Object {
        $_.PNPDeviceID -like '*VID_045E&PID_02AE*' -or $_.Name -like '*Xbox NUI*'
    }
}

if ($devices) {
    Write-Host '[OK] Windows ser följande Kinect-enheter:' -ForegroundColor Green
    $devices | Select-Object Status, Class, FriendlyName, Name | Format-Table -AutoSize
} else {
    Write-Host '[VARNING] Ingen ansluten Kinect-enhet hittades.' -ForegroundColor Yellow
    Write-Host 'Kontrollera nätadapter och USB-kabel. Prova en USB 2.0-port på datorns baksida.'
}

if (-not $sdk -or -not $devices) { exit 1 }
Write-Host '[OK] Grundkraven är uppfyllda. Sensorn kan nu provas i spelet.' -ForegroundColor Green
