@echo off
setlocal

set "PROJECT_ROOT=%~dp0"
set "APP_PATH="
for %%F in ("%PROJECT_ROOT%release\win-x64\*.exe") do if not defined APP_PATH set "APP_PATH=%%~fF"
set "PUBLISH_SCRIPT=%PROJECT_ROOT%scripts\publish.ps1"

if not defined APP_PATH (
    echo First run: publishing the application...
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PUBLISH_SCRIPT%" -OutputDirectory "%PROJECT_ROOT%release\win-x64"
    if errorlevel 1 (
        echo Publish failed. Make sure the .NET 8 SDK is installed.
        pause
        exit /b 1
    )
    for %%F in ("%PROJECT_ROOT%release\win-x64\*.exe") do if not defined APP_PATH set "APP_PATH=%%~fF"
)

if not defined APP_PATH (
    echo Application executable was not found.
    pause
    exit /b 1
)

start "" "%APP_PATH%"
endlocal
