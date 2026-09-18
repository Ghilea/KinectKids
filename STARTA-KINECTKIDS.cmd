@echo off
setlocal
set "GAME_EXE=%~dp0src\KinectKids\bin\Release\KinectKids.exe"

if not exist "%GAME_EXE%" (
    echo KinectKids maste byggas forsta gangen. Bygger nu...
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Build.ps1" -Configuration Release
    if errorlevel 1 (
        echo.
        echo Bygget misslyckades. Las felmeddelandet ovan.
        pause
        exit /b 1
    )
)

start "KinectKids" /D "%~dp0src\KinectKids\bin\Release" "%GAME_EXE%"
endlocal
