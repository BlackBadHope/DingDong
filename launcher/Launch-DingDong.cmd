@echo off
setlocal
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Launch-DingDong.ps1" %*
set "DD_RESULT=%ERRORLEVEL%"
if not "%DD_RESULT%"=="0" pause
exit /b %DD_RESULT%
