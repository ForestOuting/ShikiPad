@echo off
setlocal
if not exist "%~dp0scripts\manage_driver.ps1" (
    echo Missing scripts\manage_driver.ps1. Extract the complete ShikiPad package first.
    pause
    exit /b 1
)
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\manage_driver.ps1" -Action Install
exit /b %errorlevel%
