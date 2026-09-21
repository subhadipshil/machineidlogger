@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0lib\machineid-core.ps1" -Action current
pause
