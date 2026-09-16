@echo off
cd /d "%~dp0"
dotnet run --project Content.Client -c DebugOpt -- %*
exit /b %errorlevel%
