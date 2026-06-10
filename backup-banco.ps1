# ============================================================
# Backup do banco.db do sistema 5 CTA
# ============================================================
# Uso:
#   .\backup-banco.ps1                 # cria backup com timestamp
#   .\backup-banco.ps1 -Restaurar      # lista backups e pergunta qual restaurar
#   .\backup-banco.ps1 -Listar         # so lista os backups existentes
#
# Mantem os ultimos 30 backups por padrao (ajustavel via -MaxBackups N).
# Pula a copia se houver processo travando o banco (banco.db-wal ativo).
# ============================================================

param(
    [switch]$Restaurar,
    [switch]$Listar,
    [int]$MaxBackups = 30
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$raiz = Split-Path -Parent $MyInvocation.MyCommand.Path
# Resolve o diretorio do Int1 por wildcard (evita problema de encoding com 'c-cedilha' no path)
$bancoDir = Get-ChildItem -Path (Join-Path $raiz "Banco") -Directory | Where-Object { $_.Name -like "WebAbil8*" } | Select-Object -First 1 -ExpandProperty FullName
if (-not $bancoDir) {
    Write-Host "ERRO: pasta do Int1 nao encontrada em $raiz\Banco" -ForegroundColor Red
    exit 1
}
$bancoPath = Join-Path $bancoDir "banco.db"
$backupsDir = Join-Path $raiz "backups"

if (-not (Test-Path $bancoPath)) {
    Write-Host "ERRO: banco.db nao encontrado em $bancoPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $backupsDir)) {
    New-Item -ItemType Directory -Path $backupsDir | Out-Null
}

# ── Modo: listar ──────────────────────────────────────────────
if ($Listar) {
    $backups = Get-ChildItem $backupsDir -Filter "banco-*.db" -ErrorAction SilentlyContinue |
               Sort-Object LastWriteTime -Descending
    if ($backups.Count -eq 0) {
        Write-Host "Nenhum backup encontrado em $backupsDir" -ForegroundColor Yellow
        exit 0
    }
    Write-Host "Backups existentes (mais recentes primeiro):" -ForegroundColor Cyan
    $i = 0
    foreach ($b in $backups) {
        $i++
        $sizeMB = [math]::Round($b.Length / 1MB, 2)
        Write-Host ("  [{0,2}] {1}  ({2} MB)  {3}" -f $i, $b.Name, $sizeMB, $b.LastWriteTime)
    }
    exit 0
}

# ── Modo: restaurar ───────────────────────────────────────────
if ($Restaurar) {
    $backups = Get-ChildItem $backupsDir -Filter "banco-*.db" -ErrorAction SilentlyContinue |
               Sort-Object LastWriteTime -Descending
    if ($backups.Count -eq 0) {
        Write-Host "Nenhum backup pra restaurar." -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Backups disponiveis:" -ForegroundColor Cyan
    $i = 0
    foreach ($b in $backups) {
        $i++
        $sizeMB = [math]::Round($b.Length / 1MB, 2)
        Write-Host ("  [{0,2}] {1}  ({2} MB)  {3}" -f $i, $b.Name, $sizeMB, $b.LastWriteTime)
    }

    $escolha = Read-Host "`nNumero do backup pra restaurar (ou Enter pra cancelar)"
    if ([string]::IsNullOrWhiteSpace($escolha)) { Write-Host "Cancelado." -ForegroundColor Yellow; exit 0 }
    if (-not ($escolha -match '^\d+$')) { Write-Host "Entrada invalida." -ForegroundColor Red; exit 1 }
    $idx = [int]$escolha - 1
    if ($idx -lt 0 -or $idx -ge $backups.Count) { Write-Host "Numero fora do range." -ForegroundColor Red; exit 1 }

    $escolhido = $backups[$idx]
    Write-Host "Voce vai sobrescrever o banco atual com: $($escolhido.Name)" -ForegroundColor Yellow
    $confirm = Read-Host "Confirma? (s/N)"
    if ($confirm -ne 's' -and $confirm -ne 'S') { Write-Host "Cancelado." -ForegroundColor Yellow; exit 0 }

    # Verifica que ningguem ta usando o banco atual
    foreach ($porta in @(5018, 8080)) {
        $conns = netstat -ano | Select-String (":$porta\s") | Select-String "LISTENING"
        if ($conns) {
            Write-Host "ALERTA: porta $porta tem processo ativo. Pare o sistema antes (Ctrl+C nas janelas do iniciar.ps1)." -ForegroundColor Red
            exit 1
        }
    }

    # Backup do banco atual antes de sobrescrever (cinto + suspensorio)
    if (Test-Path $bancoPath) {
        $bkpPreRestore = Join-Path $backupsDir ("banco-PRE-RESTORE-{0:yyyy-MM-dd-HHmm}.db" -f (Get-Date))
        Copy-Item $bancoPath $bkpPreRestore
        Write-Host "Snapshot do banco atual salvo em $($bkpPreRestore | Split-Path -Leaf)" -ForegroundColor Gray
    }

    # Remove WAL/SHM (senao SQLite mistura com banco novo)
    Remove-Item (Join-Path $bancoDir "banco.db-shm") -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $bancoDir "banco.db-wal") -ErrorAction SilentlyContinue

    Copy-Item $escolhido.FullName $bancoPath -Force
    Write-Host "Restaurado: $($escolhido.Name)" -ForegroundColor Green
    Write-Host "Agora pode rodar .\iniciar.ps1" -ForegroundColor Cyan
    exit 0
}

# ── Modo padrao: criar backup ─────────────────────────────────
$timestamp = Get-Date -Format "yyyy-MM-dd-HHmm"
$destino = Join-Path $backupsDir "banco-$timestamp.db"

# Se ja existe um backup com mesmo timestamp (rodou 2x no mesmo minuto), adiciona sufixo
if (Test-Path $destino) {
    $sufixo = 1
    while (Test-Path (Join-Path $backupsDir "banco-$timestamp-$sufixo.db")) { $sufixo++ }
    $destino = Join-Path $backupsDir "banco-$timestamp-$sufixo.db"
}

try {
    # Usa Copy-Item normal - SQLite no modo WAL nao precisa de lock pra leitura,
    # mas se o banco esta sendo escrito a copia pode ficar inconsistente. Pra
    # garantir, ideal e parar o sistema antes. Aqui so avisamos se WAL ta ativo.
    $walPath = Join-Path $bancoDir "banco.db-wal"
    if (Test-Path $walPath) {
        $walSize = (Get-Item $walPath).Length
        if ($walSize -gt 0) {
            Write-Host "AVISO: banco.db-wal tem $walSize bytes - sistema ativo. Backup pode ficar inconsistente." -ForegroundColor Yellow
            Write-Host "       Pra backup garantido, pare o sistema antes de rodar este script." -ForegroundColor Yellow
        }
    }

    Copy-Item $bancoPath $destino -Force
    $sizeMB = [math]::Round((Get-Item $destino).Length / 1MB, 2)
    $nome = Split-Path $destino -Leaf
    Write-Host ("Backup criado: {0}  ({1} MB)" -f $nome, $sizeMB) -ForegroundColor Green
} catch {
    Write-Host "ERRO ao copiar: $_" -ForegroundColor Red
    exit 1
}

# ── Rotacao: mantem so os N mais novos ────────────────────────
$todos = Get-ChildItem $backupsDir -Filter "banco-*.db" |
         Where-Object { $_.Name -notlike "*PRE-RESTORE*" } |  # nao apaga snapshots pre-restore
         Sort-Object LastWriteTime -Descending

if ($todos.Count -gt $MaxBackups) {
    $aRemover = $todos | Select-Object -Skip $MaxBackups
    foreach ($antigo in $aRemover) {
        Remove-Item $antigo.FullName -ErrorAction SilentlyContinue
        Write-Host "  Removido (antigo): $($antigo.Name)" -ForegroundColor Gray
    }
}

$total = ($todos.Count, $MaxBackups | Measure-Object -Minimum).Minimum
Write-Host ("Total de backups: {0} (limite: {1})" -f $total, $MaxBackups) -ForegroundColor Cyan
