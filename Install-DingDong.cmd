@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-DingDong.ps1" %*
if errorlevel 1 pause
