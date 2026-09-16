@echo off
cd /d "%~dp0"
dotnet run --project Content.Server -c DebugOpt -- %*
exit /b %errorlevel%
