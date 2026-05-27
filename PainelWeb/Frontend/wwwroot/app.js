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
// Exibe as 24 horas completas do dia (00h–23h).
// Chamado por Home.razor via OnAfterRenderAsync (criação) e pelo auto-refresh
// (atualização via updateDashboardChart, sem recriar o canvas).
//
// permitidosData / negadosData: arrays de 24 inteiros (índice = hora 0–23),
// com valores em horário LOCAL do servidor (UTC-3 para Brasília).
// -----------------------------------------------------------------------------

// Referência à instância ativa — permite atualização sem recriar o canvas
let _dashboardChart = null;

/**
 * Cria (ou recria) o gráfico de acessos no dashboard.
 * @param {string}   canvasId       - ID do elemento <canvas> no DOM
 * @param {number[]} permitidosData - Array 24 inteiros (índice = hora local)
 * @param {number[]} negadosData    - Array 24 inteiros (índice = hora local)
 */
window.renderDashboardChart = function(canvasId, permitidosData, negadosData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !window.Chart) return;

    // Destroi instância anterior para evitar erro "Canvas is already in use"
    // ao navegar para fora e voltar ao Dashboard
    if (_dashboardChart) { _dashboardChart.destroy(); _dashboardChart = null; }
    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const ctx   = canvas.getContext('2d');
    const style = getComputedStyle(document.documentElement);

    // Paleta do design system (variáveis CSS) — fallback caso variável não exista
    const green     = style.getPropertyValue('--chart-1').trim() || '#34d399';
    const red       = style.getPropertyValue('--chart-3').trim() || '#ef4444';
    const gridLine  = style.getPropertyValue('--border').trim()  || '#2d3a52';
    const textMuted = style.getPropertyValue('--muted-foreground').trim() || '#8b95a8';

    // Gradientes verticais: cor sólida no topo → totalmente transparente na base
    const h = canvas.offsetHeight || 320;
    const gradGreen = ctx.createLinearGradient(0, 0, 0, h);
    gradGreen.addColorStop(0, green + 'BB');
    gradGreen.addColorStop(1, green + '00');
    const gradRed = ctx.createLinearGradient(0, 0, 0, h);
    gradRed.addColorStop(0, red + 'BB');
    gradRed.addColorStop(1, red + '00');

    // 24 rótulos completos — 00:00 até 23:00
    const labels     = Array.from({length: 24}, (_, i) => i.toString().padStart(2, '0') + ':00');
    const permitidos = Array.isArray(permitidosData) ? permitidosData.slice() : new Array(24).fill(0);
    const negados    = Array.isArray(negadosData)    ? negadosData.slice()    : new Array(24).fill(0);

    // Hora atual no navegador — ponto maior destaca a hora em curso
    const horaAtual = new Date().getHours();
    const makeRadius = (data) =>
        data.map((v, i) => i === horaAtual ? 7 : (v > 0 ? 4 : 2));

    _dashboardChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels,
            datasets: [
                {
                    label: 'Permitidos',
                    data: permitidos,
                    borderColor: green,
                    backgroundColor: gradGreen,
                    fill: true,
                    tension: 0.4,
                    borderWidth: 2.5,
                    pointRadius: makeRadius(permitidos),
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
                    data: negados,
                    borderColor: red,
                    backgroundColor: gradRed,
                    fill: true,
                    tension: 0.4,
                    borderWidth: 2.5,
                    pointRadius: makeRadius(negados),
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
            // Tooltip simultâneo para ambos os datasets ao passar o mouse
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
                        // Cabeçalho: "18:00 → 18:59   ◀ agora" quando é a hora atual
                        title: (items) => {
                            const h   = parseInt(items[0].label, 10);
                            const fim = (h + 1).toString().padStart(2, '0') + ':59';
                            return `${items[0].label} → ${fim}${h === horaAtual ? '   ◀ agora' : ''}`;
                        },
                        // Corpo: "  Permitidos: 3 (75%)"
                        label: (item) => {
                            const idx   = item.dataIndex;
                            const total = permitidos[idx] + negados[idx];
                            const pct   = total > 0 ? Math.round(item.raw / total * 100) : 0;
                            return `  ${item.dataset.label}: ${item.raw}${total > 0 ? ' (' + pct + '%)' : ''}`;
                        },
                        // Rodapé: total de acessos e taxa de liberação
                        afterBody: (items) => {
                            const idx   = items[0].dataIndex;
                            const total = permitidos[idx] + negados[idx];
                            if (total === 0) return [];
                            const taxa = Math.round(permitidos[idx] / total * 100);
                            return ['', `  Total: ${total}   ·   Liberação: ${taxa}%`];
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
                        font: { size: 11 },
                        // Exibe rótulo a cada 3 horas: 00:00, 03:00, 06:00 … 21:00
                        callback: (val, idx) => idx % 3 === 0 ? labels[idx] : ''
                    }
                },
                y: {
                    grid:   { color: gridLine + '33', drawBorder: false },
                    border: { display: false },
                    beginAtZero: true,
                    ticks: {
                        color: textMuted,
                        precision: 0,       // Apenas inteiros no eixo Y
                        font: { size: 11 }
                    }
                }
            }
        }
    });
};

/**
 * Atualiza os dados do gráfico sem recriar o canvas.
 * Usado pelo auto-refresh e pelo botão de atualização manual do Dashboard.
 * A animação 'active' é mais suave para atualizações incrementais.
 * @param {number[]} permitidosData - Array 24 inteiros
 * @param {number[]} negadosData    - Array 24 inteiros
 */
window.updateDashboardChart = function(permitidosData, negadosData) {
    if (!_dashboardChart) return;
    const permitidos = Array.isArray(permitidosData) ? permitidosData.slice() : new Array(24).fill(0);
    const negados    = Array.isArray(negadosData)    ? negadosData.slice()    : new Array(24).fill(0);
    _dashboardChart.data.datasets[0].data = permitidos;
    _dashboardChart.data.datasets[1].data = negados;
    _dashboardChart.update('active');
};
