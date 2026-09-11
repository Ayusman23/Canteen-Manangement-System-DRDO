# DRDO Canteen Management System - Enterprise Launch Script

Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  DRDO CANTEEN MANAGEMENT SYSTEM - ENTERPRISE EDITION  " -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Start Backend
Write-Host "Launching Enterprise Backend (.NET 9.0 Clean Architecture)..." -ForegroundColor Green
$backendDir = "Canteen management\Canteen management\CanteenApi"
Start-Process dotnet -ArgumentList "run --project `"$backendDir`"" -NoNewWindow
Start-Sleep -Seconds 4

# 2. Display Pre-seeded Evaluation Credentials
Write-Host ""
Write-Host "PRE-SEEDED EVALUATION CREDENTIALS:" -ForegroundColor Yellow
Write-Host "  1. Canteen Manager:    manager@drdo.gov.in  / Manager@DRDO2026!" -ForegroundColor White
Write-Host "  2. Kitchen Operator:   kitchen@drdo.gov.in  / Kitchen@DRDO2026!" -ForegroundColor White
Write-Host "  3. Employee:           employee@drdo.gov.in / Employee@DRDO2026!" -ForegroundColor White
Write-Host ""
Write-Host "API Swagger Documentation: http://localhost:5170/swagger" -ForegroundColor Gray
Write-Host "Health Check Endpoint:    http://localhost:5170/health/live" -ForegroundColor Gray
Write-Host "SignalR Telemetry Hub:    http://localhost:5170/hubs/canteen" -ForegroundColor Gray
Write-Host ""

# 3. Start Frontend
Write-Host "Launching Frontend Portal (React 19 + Vite + SignalR)..." -ForegroundColor Green
Set-Location frontend
Write-Host "Frontend Portal will open at http://localhost:5173" -ForegroundColor Cyan
Start-Process "http://localhost:5173"
npm run dev
