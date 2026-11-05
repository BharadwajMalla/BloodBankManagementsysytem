@echo off
title Blood Bank Angular Client
echo ===================================================
echo Starting Blood Bank Frontend on http://localhost:4200
echo ===================================================
cd /d "%~dp0frontend\blood-bank-client"
npm start
pause
