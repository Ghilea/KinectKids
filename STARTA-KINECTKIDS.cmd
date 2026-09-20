@echo off
setlocal
set "GAME_DIR=%LOCALAPPDATA%\KinectKids\Build\KinectKids"
set "GAME_EXE=%GAME_DIR%\KinectKids.exe"

if not exist "%GAME_EXE%" (
    echo KinectKids Unity-version maste byggas forsta gangen. Bygger nu...
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\Build.ps1"
    if errorlevel 1 (
        echo.
        echo Unity-bygget misslyckades. Las felmeddelandet ovan.
        pause
        exit /b 1
    )
)

start "KinectKids" /D "%GAME_DIR%" "%GAME_EXE%"
endlocal
