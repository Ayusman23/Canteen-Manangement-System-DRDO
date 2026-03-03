# DRDO Canteen Management System - Run Script

Write-Host "Starting DRDO Canteen Management System..." -ForegroundColor Cyan

# 1. Start Backend
Write-Host "Launching Backend (ASP.NET Core)..." -ForegroundColor Green
$backendDir = "Canteen management\Canteen management\CanteenApi"
Start-Process dotnet -ArgumentList "run --project `"$backendDir`"" -NoNewWindow
Start-Sleep -Seconds 5

# 2. Start Frontend
Write-Host "Launching Frontend (React + Vite)..." -ForegroundColor Green
Set-Location frontend
Write-Host "The application will be available at http://localhost:5173" -ForegroundColor Yellow
Start-Process "http://localhost:5173"
npm run dev
