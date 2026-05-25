[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " Sistema de Controle de Acesso Biometrico 5 CTA" -ForegroundColor Cyan
Write-Host " Iniciando todos os servicos..." -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

$raiz = Split-Path -Parent $MyInvocation.MyCommand.Path

$int1 = Get-ChildItem -Path (Join-Path $raiz "Banco") -Directory | Where-Object { $_.Name -like "WebAbil8*" } | Select-Object -First 1 -ExpandProperty FullName
$int2Parent = Get-ChildItem -Path (Join-Path $raiz "Hardware*") -Directory -Recurse | Where-Object { $_.Name -eq "BiometricAcess.Worker" } | Select-Object -First 1 -ExpandProperty FullName
$int2 = Join-Path $int2Parent "BiometricAcess.Worker"
$int3 = Join-Path $raiz "PainelWeb\Frontend"

Write-Host "[1/3] Iniciando Int1 - Banco API: $int1" -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int1'; dotnet run --launch-profile https"

Write-Host "Aguardando banco inicializar (8s)..." -ForegroundColor Yellow
Start-Sleep -Seconds 8

Write-Host "[2/3] Iniciando Int2 - Worker: $int2" -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int2'; dotnet run"

Write-Host "[3/3] Iniciando Int3 - Painel Web: $int3" -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int3'; dotnet run"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " Todos os servicos iniciados!" -ForegroundColor Cyan
Write-Host " Int1 - Banco API: https://localhost:7117" -ForegroundColor Cyan
Write-Host " Int3 - Painel Web: http://localhost:8080" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Read-Host "Pressione Enter para fechar"