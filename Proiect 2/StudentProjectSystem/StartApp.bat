@echo off
cd /d "%~dp0"
echo Pornire server...
start "" "https://localhost:61977"
dotnet run
pause
