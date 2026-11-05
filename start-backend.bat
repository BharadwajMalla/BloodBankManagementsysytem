@echo off
title Blood Bank Backend API
echo ===================================================
echo Starting Blood Bank .NET Backend on http://localhost:5105
echo ===================================================
cd /d "%~dp0backend\BloodBank.API"
dotnet run --launch-profile http
pause
