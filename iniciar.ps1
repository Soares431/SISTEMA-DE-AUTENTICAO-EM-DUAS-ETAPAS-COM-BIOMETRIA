[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "================================================" -ForegroundColor Cyan
Write-Host " Sistema de Controle de Acesso Biometrico 5 CTA" -ForegroundColor Cyan
Write-Host " Iniciando todos os servicos..." -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan

$raiz = Split-Path -Parent $MyInvocation.MyCommand.Path

# Encerra processos antigos nas portas 5018, 8080 (e 7117 legado) para evitar conflito
Write-Host "Verificando portas em uso..." -ForegroundColor Yellow
foreach ($porta in @(5018, 7117, 8080)) {
    $conns = netstat -ano | Select-String (":$porta\s") | Select-String "LISTENING"
    if ($conns) {
        $conns | ForEach-Object {
            $parts = ($_.ToString() -split '\s+')
            $pidVal = $parts[-1]
            if ($pidVal -match '^\d+$' -and $pidVal -ne '0') {
                Write-Host "  Encerrando processo PID $pidVal na porta $porta..." -ForegroundColor Yellow
                try { Stop-Process -Id ([int]$pidVal) -Force -ErrorAction SilentlyContinue } catch {}
            }
        }
        Start-Sleep -Seconds 1
    }
}

$int1 = Get-ChildItem -Path (Join-Path $raiz "Banco") -Directory | Where-Object { $_.Name -like "WebAbil8*" } | Select-Object -First 1 -ExpandProperty FullName
$int2Parent = Get-ChildItem -Path (Join-Path $raiz "Hardware*") -Directory -Recurse | Where-Object { $_.Name -eq "BiometricAcess.Worker" } | Select-Object -First 1 -ExpandProperty FullName
$int2 = Join-Path $int2Parent "BiometricAcess.Worker"
$int3 = Join-Path $raiz "PainelWeb\Frontend"

# Int1 usa --launch-profile http (porta 5018 apenas, sem HTTPS)
# Evita problema de certificado dev nao configurado que impedia o Int1 de subir
Write-Host "[1/3] Iniciando Int1 - Banco API (em segundo plano)..." -ForegroundColor Green
$p1 = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int1'; dotnet run --launch-profile http" -WindowStyle Hidden -PassThru

# Aguarda Int1 responder na porta 5018 (ate 90 segundos)
Write-Host "      Aguardando Int1 na porta 5018 " -ForegroundColor Yellow -NoNewline
$tentativas = 0
$int1Ok = $false
while ($tentativas -lt 90) {
    $tcp = Test-NetConnection -ComputerName localhost -Port 5018 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($tcp) { $int1Ok = $true; break }
    Start-Sleep -Seconds 1
    Write-Host "." -NoNewline -ForegroundColor Yellow
    $tentativas++
}
if ($int1Ok) {
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " TIMEOUT (Int1 nao respondeu em 90s)" -ForegroundColor Red
    Write-Host ""
    Write-Host "ERRO CRITICO: O Int1 (Banco API) nao iniciou." -ForegroundColor Red
    Write-Host "Verifique erros de compilacao ou conflitos de porta." -ForegroundColor Red
    Write-Host "Encerrando. Pressione Enter para sair." -ForegroundColor Red
    Read-Host
    exit 1
}

Write-Host "[2/3] Iniciando Int2 - Worker (em segundo plano)..." -ForegroundColor Green
$p2 = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int2'; dotnet run" -WindowStyle Hidden -PassThru

Write-Host "[3/3] Iniciando Int3 - Painel Web (em segundo plano)..." -ForegroundColor Green
$p3 = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int3'; dotnet run" -WindowStyle Hidden -PassThru

# Aguarda Int3 responder na porta 8080 (ate 150 segundos, tempo extra para compilacao apos alteracoes)
Write-Host "      Aguardando Int3 na porta 8080 " -ForegroundColor Yellow -NoNewline
$tentativas = 0
$int3Ok = $false
while ($tentativas -lt 150) {
    $tcp = Test-NetConnection -ComputerName localhost -Port 8080 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($tcp) { $int3Ok = $true; break }
    Start-Sleep -Seconds 1
    Write-Host "." -NoNewline -ForegroundColor Yellow
    $tentativas++
}
if ($int3Ok) {
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " TIMEOUT - Int3 nao respondeu em 150s." -ForegroundColor Red
    Write-Host "Abrindo janela visivel do Int3 para diagnostico de erros..." -ForegroundColor Yellow
    if ($null -ne $p3 -and -not $p3.HasExited) {
        try { Stop-Process -Id $p3.Id -Force -ErrorAction SilentlyContinue } catch {}
    }
    $p3 = Start-Process powershell -ArgumentList "-NoExit", "-Command", "Set-Location '$int3'; dotnet run; Read-Host Pressione Enter" -WindowStyle Normal -PassThru
    Write-Host "Janela do Int3 aberta - verifique o erro exibido." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan
Write-Host " Todos os servicos iniciados!" -ForegroundColor Cyan
Write-Host " Int1 - Banco API : http://localhost:5018" -ForegroundColor Cyan
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
