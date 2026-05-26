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

# -PassThru retorna o objeto de processo para poder encerrar depois
Write-Host "[1/3] Iniciando Int1 - Banco API (em segundo plano)..." -ForegroundColor Green
$p1 = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int1'; dotnet run --launch-profile https" -WindowStyle Hidden -PassThru

# Aguarda Int1 responder na porta 7117
Write-Host "      Aguardando Int1 na porta 7117 " -ForegroundColor Yellow -NoNewline
$tentativas = 0
while ($tentativas -lt 60) {
    $tcp = Test-NetConnection -ComputerName localhost -Port 7117 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($tcp) { break }
    Start-Sleep -Seconds 1
    Write-Host "." -NoNewline -ForegroundColor Yellow
    $tentativas++
}
Write-Host " OK" -ForegroundColor Green

Write-Host "[2/3] Iniciando Int2 - Worker (em segundo plano)..." -ForegroundColor Green
$p2 = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int2'; dotnet run" -WindowStyle Hidden -PassThru

Write-Host "[3/3] Iniciando Int3 - Painel Web (em segundo plano)..." -ForegroundColor Green
$p3 = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int3'; dotnet run" -WindowStyle Hidden -PassThru

# Aguarda Int3 responder na porta 8080
Write-Host "      Aguardando Int3 na porta 8080 " -ForegroundColor Yellow -NoNewline
$tentativas = 0
while ($tentativas -lt 60) {
    $tcp = Test-NetConnection -ComputerName localhost -Port 8080 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($tcp) { break }
    Start-Sleep -Seconds 1
    Write-Host "." -NoNewline -ForegroundColor Yellow
    $tentativas++
}
Write-Host " OK" -ForegroundColor Green

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host " Todos os servicos iniciados!" -ForegroundColor Cyan
Write-Host " Int1 - Banco API : https://localhost:7117" -ForegroundColor Cyan
Write-Host " Int3 - Painel Web: http://localhost:8080" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Abre o navegador padrao automaticamente
Write-Host "Abrindo painel no navegador..." -ForegroundColor Green
Start-Process "http://localhost:8080"

Write-Host ""
Read-Host "Pressione Enter para encerrar todos os servicos"

# Encerra as arvores de processo iniciadas por este script
Write-Host "Encerrando servicos..." -ForegroundColor Yellow
foreach ($p in @($p1, $p2, $p3)) {
    if ($null -ne $p -and -not $p.HasExited) {
        # /T encerra o processo e todos os filhos (dotnet run spawna processos filhos)
        & taskkill /PID $p.Id /T /F 2>$null | Out-Null
    }
}
Write-Host "Servicos encerrados." -ForegroundColor Green
