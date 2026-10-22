@echo off
setlocal
cd /d "%~dp0"
set "DD_PS=%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
if exist "%ProgramFiles%\PowerShell\7\pwsh.exe" set "DD_PS=%ProgramFiles%\PowerShell\7\pwsh.exe"
"%DD_PS%" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0DingDong-Update.ps1" %*
set "DD_RESULT=%errorlevel%"
echo.
if not "%DD_RESULT%"=="0" echo DingDong updater stopped with error %DD_RESULT%.
if not defined DD_NO_PAUSE pause
exit /b %DD_RESULT%
