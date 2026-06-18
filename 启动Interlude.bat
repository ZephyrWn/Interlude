@echo off
setlocal

pushd "%~dp0" >nul
if errorlevel 1 (
    echo Failed to switch to the batch file directory.
    echo.
    pause
    exit /b 1
)

set "POWERSHELL_EXE=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"

if not exist "%POWERSHELL_EXE%" (
    echo PowerShell was not found at:
    echo %POWERSHELL_EXE%
    echo.
    echo Please check your Windows installation.
    echo.
    pause
    popd >nul
    exit /b 1
)

"%POWERSHELL_EXE%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-Interlude.ps1" %*
set "EXIT_CODE=%ERRORLEVEL%"

if not "%EXIT_CODE%"=="0" (
    echo.
    echo Failed to start Interlude. Exit code: %EXIT_CODE%
    echo Please check the error messages above.
    echo.
    pause
)

popd >nul
exit /b %EXIT_CODE%
