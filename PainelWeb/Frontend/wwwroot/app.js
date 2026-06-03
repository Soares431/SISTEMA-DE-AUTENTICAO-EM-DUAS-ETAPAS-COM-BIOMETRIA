// =============================================================================
// app.js - Interoperabilidade JavaScript (JS Interop) do Blazor
// Sistema de Controle de Acesso Biométrico do 5° CTA
// Responsável por: gerenciamento de tema (dark/light) e renderização do gráfico
// =============================================================================

// -----------------------------------------------------------------------------
// Gerenciador de Tema (Dark/Light Mode)
// Utiliza localStorage para persistência e respeita preferência do sistema.
// Chamado pelo componente MainLayout.razor via IJSRuntime.
// -----------------------------------------------------------------------------
window.themeManager = {
    /**
     * Define o tema ativo e persiste no localStorage.
     * @param {string} theme - "dark" ou "light"
     */
    setTheme: function(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
    },

    /**
     * Retorna o tema atual. Prioriza localStorage, depois preferência do SO.
     * @returns {string} "dark" ou "light"
     */
    getTheme: function() {
        return localStorage.getItem('theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    },

    /**
     * Inicializa o tema ao carregar a página (chamado automaticamente).
     */
    initTheme: function() {
        const theme = this.getTheme();
        document.documentElement.setAttribute('data-theme', theme);
    }
};

// Inicializa o tema imediatamente ao carregar o script
window.themeManager.initTheme();

// -----------------------------------------------------------------------------
// Persistência de autenticação no sessionStorage
// Usado para restaurar o token JWT se o circuito Blazor reconectar durante a sessão.
// sessionStorage é limpo automaticamente ao fechar a aba/navegador.
// -----------------------------------------------------------------------------
window.authStorage = {
    save: function(token, adminId, nome) {
        sessionStorage.setItem('cta_token', token);
        sessionStorage.setItem('cta_adminId', String(adminId));
        sessionStorage.setItem('cta_nome', nome);
    },
    load: function() {
        var token = sessionStorage.getItem('cta_token');
        if (!token) return null;
        return {
            token: token,
            adminId: parseInt(sessionStorage.getItem('cta_adminId') || '0', 10),
            nome: sessionStorage.getItem('cta_nome') || ''
        };
    },
    clear: function() {
        sessionStorage.removeItem('cta_token');
        sessionStorage.removeItem('cta_adminId');
        sessionStorage.removeItem('cta_nome');
    }
};

// -----------------------------------------------------------------------------
// Download de CSV
// Dispara download de um arquivo CSV no navegador do usuário.
// Chamado por Historico.razor e Logs.razor via IJSRuntime.
// -----------------------------------------------------------------------------
// Abre a gravação MP4 de uma tentativa em nova aba — usa o JWT do TokenStore
// porque o endpoint /api/gravacoes/{id} é [Authorize].
window.abrirGravacao = async function(apiBaseUrl, tentativaId, token) {
    try {
        var resp = await fetch(apiBaseUrl + 'api/gravacoes/' + tentativaId, {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (!resp.ok) {
            alert('Não foi possível abrir a gravação (HTTP ' + resp.status + ').');
            return;
        }
        var blob = await resp.blob();
        var url = URL.createObjectURL(blob);
        window.open(url, '_blank');
        // libera depois de 1min — tempo de o navegador começar a reproduzir
        setTimeout(function(){ URL.revokeObjectURL(url); }, 60000);
    } catch (e) {
        alert('Erro ao abrir gravação: ' + e.message);
    }
};

// Baixa um arquivo PDF autenticado e dispara download no navegador.
window.baixarPdf = async function(url, token, filename) {
    try {
        var resp = await fetch(url, { headers: { 'Authorization': 'Bearer ' + token } });
        if (!resp.ok) {
            alert('Falha ao gerar PDF (HTTP ' + resp.status + ').');
            return;
        }
        var blob = await resp.blob();
        var blobUrl = URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = blobUrl;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(blobUrl);
    } catch (e) {
        alert('Erro ao baixar PDF: ' + e.message);
    }
};

window.downloadCsv = function(filename, csvContent) {
    var blob = new Blob(['﻿' + csvContent], { type: 'text/csv;charset=utf-8;' });
    var url  = URL.createObjectURL(blob);
    var a    = document.createElement('a');
    a.href     = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// -----------------------------------------------------------------------------
// Gráfico do Dashboard — Acessos por Hora (Chart.js)
//
// Estado em variáveis de módulo (não em closure) para que updateDashboardChart
// possa recalcular _horaAtual sem precisar recriar o gráfico inteiro.
//
// BUG ANTERIOR: _horaAtual era capturada como variável local no momento de
// renderDashboardChart — se o usuário ficasse na página das 16h às 19h, o
// marcador "agora" continuaria apontando para as 16h mesmo após updates.
// CORREÇÃO: _horaAtual é variável de módulo, atualizada em todo update.
// -----------------------------------------------------------------------------

// --- Estado do gráfico (módulo-level para sobreviver entre updates) ----------
let _dashboardChart = null;          // Instância Chart.js ativa
let _horaAtual      = -1;            // Hora atual do navegador; -1 = não inicializado
let _permitidos     = new Array(24).fill(0);   // Cacheado para os callbacks do tooltip
let _negados        = new Array(24).fill(0);   // Cacheado para os callbacks do tooltip
let _clockInterval  = null;          // ID do setInterval do relógio em tempo real
// -----------------------------------------------------------------------------

/**
 * Retorna array de raios dos pontos: 7 na hora atual, 4 se há dados, 2 se vazio.
 * Recebe os dados e a hora atual como parâmetros (sem depender de closure).
 */
function _makeRadius(data, hora) {
    return data.map(function(v, i) {
        return i === hora ? 7 : (v > 0 ? 4 : 2);
    });
}

/**
 * Cria (ou recria) o gráfico de acessos no dashboard.
 * @param {string}   canvasId       - ID do elemento <canvas> no DOM
 * @param {number[]} permitidosData - Array 24 inteiros (índice = hora local 0-23)
 * @param {number[]} negadosData    - Array 24 inteiros (índice = hora local 0-23)
 */
window.renderDashboardChart = function(canvasId, permitidosData, negadosData) {
    var canvas = document.getElementById(canvasId);
    if (!canvas || !window.Chart) return;

    // Destroi instância anterior para evitar erro "Canvas is already in use"
    // ao navegar para fora e voltar ao Dashboard
    if (_dashboardChart) { _dashboardChart.destroy(); _dashboardChart = null; }
    var existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    var ctx   = canvas.getContext('2d');
    var style = getComputedStyle(document.documentElement);

    // Paleta do design system (variáveis CSS) — fallback caso variável não exista
    var green     = style.getPropertyValue('--chart-1').trim() || '#34d399';
    var red       = style.getPropertyValue('--chart-3').trim() || '#ef4444';
    var gridLine  = style.getPropertyValue('--border').trim()  || '#2d3a52';
    var textMuted = style.getPropertyValue('--muted-foreground').trim() || '#8b95a8';

    // Gradientes verticais: cor sólida no topo, transparente na base
    var h = canvas.offsetHeight || 320;
    var gradGreen = ctx.createLinearGradient(0, 0, 0, h);
    gradGreen.addColorStop(0, green + 'BB');
    gradGreen.addColorStop(1, green + '00');
    var gradRed = ctx.createLinearGradient(0, 0, 0, h);
    gradRed.addColorStop(0, red + 'BB');
    gradRed.addColorStop(1, red + '00');

    // Atualiza estado do módulo — tooltip callbacks referenciam essas vars
    _horaAtual  = new Date().getHours();
    _permitidos = Array.isArray(permitidosData) ? permitidosData.slice() : new Array(24).fill(0);
    _negados    = Array.isArray(negadosData)    ? negadosData.slice()    : new Array(24).fill(0);

    // 24 rótulos — 00 até 23
    var labels = [];
    for (var i = 0; i < 24; i++) {
        labels.push(i.toString().padStart(2, '0'));
    }

    _dashboardChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Permitidos',
                    data: _permitidos,
                    borderColor: green,
                    backgroundColor: gradGreen,
                    fill: true,
                    tension: 0.4,
                    borderWidth: 2.5,
                    pointRadius: _makeRadius(_permitidos, _horaAtual),
                    pointHoverRadius: 8,
                    pointBackgroundColor: green,
                    pointBorderColor: '#0f1729',
                    pointBorderWidth: 2,
                    pointHoverBackgroundColor: green,
                    pointHoverBorderColor: '#ffffff',
                    pointHoverBorderWidth: 2,
                },
                {
                    label: 'Negados',
                    data: _negados,
                    borderColor: red,
                    backgroundColor: gradRed,
                    fill: true,
                    tension: 0.4,
                    borderWidth: 2.5,
                    pointRadius: _makeRadius(_negados, _horaAtual),
                    pointHoverRadius: 8,
                    pointBackgroundColor: red,
                    pointBorderColor: '#0f1729',
                    pointBorderWidth: 2,
                    pointHoverBackgroundColor: red,
                    pointHoverBorderColor: '#ffffff',
                    pointHoverBorderWidth: 2,
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            animation: { duration: 700, easing: 'easeInOutQuart' },
            // Tooltip simultâneo para ambos os datasets
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: {
                    labels: {
                        color: textMuted,
                        usePointStyle: true,
                        pointStyleWidth: 12,
                        padding: 24,
                        font: { size: 12, weight: '500' }
                    }
                },
                tooltip: {
                    backgroundColor: 'rgba(15, 23, 42, 0.97)',
                    titleColor: '#ffffff',
                    bodyColor: textMuted,
                    borderColor: gridLine,
                    borderWidth: 1,
                    padding: { x: 14, y: 10 },
                    cornerRadius: 8,
                    displayColors: true,
                    boxWidth: 10,
                    boxHeight: 10,
                    callbacks: {
                        // Usa _horaAtual (módulo-level) — sempre atualizada pelo updateDashboardChart
                        title: function(items) {
                            var h = parseInt(items[0].label, 10);
                            var agora = h === _horaAtual ? '   ◄ agora' : '';
                            return 'Hora ' + items[0].label + 'h' + agora;
                        },
                        label: function(item) {
                            var idx   = item.dataIndex;
                            var total = _permitidos[idx] + _negados[idx];
                            var pct   = total > 0 ? Math.round(item.raw / total * 100) : 0;
                            var pctStr = total > 0 ? ' (' + pct + '%)' : '';
                            return '  ' + item.dataset.label + ': ' + item.raw + pctStr;
                        },
                        afterBody: function(items) {
                            var idx   = items[0].dataIndex;
                            var total = _permitidos[idx] + _negados[idx];
                            if (total === 0) return [];
                            var taxa = Math.round(_permitidos[idx] / total * 100);
                            return ['', '  Total: ' + total + '   ·   Liberação: ' + taxa + '%'];
                        }
                    }
                }
            },
            scales: {
                x: {
                    grid:   { color: gridLine + '33', drawBorder: false },
                    border: { display: false },
                    ticks: {
                        color: textMuted,
                        maxRotation: 0,
                        autoSkip: false,
                        font: { size: 11 }
                        // Mostra todas as 24 horas (00, 01, ..., 23) — Chart.js pula automaticamente se faltar espaço
                    }
                },
                y: {
                    grid:   { color: gridLine + '33', drawBorder: false },
                    border: { display: false },
                    beginAtZero: true,
                    ticks: {
                        color: textMuted,
                        precision: 0,      // Apenas inteiros no eixo Y
                        font: { size: 11 }
                    }
                }
            }
        }
    });
};

/**
 * Atualiza dados do gráfico sem recriar o canvas.
 * IMPORTANTE: recalcula _horaAtual para que o marcador "agora" e o tooltip
 * reflitam a hora correta mesmo horas após a criação do gráfico.
 * @param {number[]} permitidosData - Array 24 inteiros
 * @param {number[]} negadosData    - Array 24 inteiros
 */
window.updateDashboardChart = function(permitidosData, negadosData) {
    if (!_dashboardChart) return;

    // Recalcula hora atual — corrige o marcador caso a hora tenha mudado
    _horaAtual  = new Date().getHours();
    _permitidos = Array.isArray(permitidosData) ? permitidosData.slice() : new Array(24).fill(0);
    _negados    = Array.isArray(negadosData)    ? negadosData.slice()    : new Array(24).fill(0);

    _dashboardChart.data.datasets[0].data        = _permitidos;
    _dashboardChart.data.datasets[1].data        = _negados;
    // Atualiza raio dos pontos para refletir a hora atual correta
    _dashboardChart.data.datasets[0].pointRadius = _makeRadius(_permitidos, _horaAtual);
    _dashboardChart.data.datasets[1].pointRadius = _makeRadius(_negados,    _horaAtual);

    // 'active' = animação suave para atualizações incrementais (sem flash)
    _dashboardChart.update('active');
};

/**
 * Inicia um relógio em tempo real (HH:MM:SS) dentro de um elemento DOM.
 * Atualiza a cada segundo via setInterval — zero re-renders do Blazor.
 * Auto-limpa quando o elemento some do DOM (ex: navegação para outra página).
 * @param {string} elementId - ID do elemento <span> que receberá o horário
 */
window.startDashboardClock = function(elementId) {
    // Para qualquer relógio anterior antes de iniciar um novo
    if (_clockInterval) { clearInterval(_clockInterval); _clockInterval = null; }

    function tick() {
        var el = document.getElementById(elementId);
        if (!el) {
            // Elemento sumiu do DOM (navegação) — limpa o intervalo automaticamente
            clearInterval(_clockInterval);
            _clockInterval = null;
            return;
        }
        var now = new Date();
        var hh  = now.getHours()  .toString().padStart(2, '0');
        var mm  = now.getMinutes().toString().padStart(2, '0');
        var ss  = now.getSeconds().toString().padStart(2, '0');
        el.textContent = hh + ':' + mm + ':' + ss;
    }

    tick(); // Atualização imediata (sem esperar 1s)
    _clockInterval = setInterval(tick, 1000);
};
