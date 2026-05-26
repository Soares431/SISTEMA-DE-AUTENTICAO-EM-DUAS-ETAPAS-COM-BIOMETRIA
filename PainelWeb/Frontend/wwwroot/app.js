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
// Gráfico do Dashboard - Acessos por Hora (Chart.js)
// Renderiza um gráfico de linhas com dados de acessos permitidos e negados.
// Chamado pelo componente Home.razor via OnAfterRenderAsync.
// Utiliza variáveis CSS do design system para cores consistentes com o tema.
// -----------------------------------------------------------------------------

/**
 * Renderiza o gráfico de acessos no dashboard.
 * @param {string} canvasId - ID do elemento <canvas> no DOM
 */
// Dispara download de um arquivo CSV no navegador do usuário
window.downloadCsv = function(filename, csvContent) {
    var blob = new Blob(['﻿' + csvContent], { type: 'text/csv;charset=utf-8;' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// permitidosData e negadosData: arrays de 24 inteiros (índice = hora do dia)
window.renderDashboardChart = function(canvasId, permitidosData, negadosData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas || !window.Chart) return;

    const ctx = canvas.getContext('2d');

    // Lê as cores do design system (variáveis CSS) para manter consistência com o tema
    const style = getComputedStyle(document.documentElement);
    const primary = style.getPropertyValue('--chart-1').trim() || '#34d399';       // Verde - acessos permitidos
    const destructive = style.getPropertyValue('--chart-3').trim() || '#ef4444';   // Vermelho - acessos negados
    const border = style.getPropertyValue('--border').trim() || '#2d3a52';         // Cor das linhas do grid
    const muted = style.getPropertyValue('--muted-foreground').trim() || '#8b95a8'; // Cor dos rótulos

    // Horários do expediente (06h às 16h) — índices 6 a 16
    const labels = ['06:00','07:00','08:00','09:00','10:00','11:00','12:00','13:00','14:00','15:00','16:00'];
    const permitidos = (permitidosData || new Array(24).fill(0)).slice(6, 17);
    const negados    = (negadosData    || new Array(24).fill(0)).slice(6, 17);

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'Permitidos',
                    data: permitidos,
                    borderColor: primary,
                    backgroundColor: primary + '4D', // 30% de opacidade para o preenchimento
                    fill: true,
                    tension: 0.4,    // Suaviza as curvas do gráfico
                    borderWidth: 2
                },
                {
                    label: 'Negados',
                    data: negados,
                    borderColor: destructive,
                    backgroundColor: destructive + '4D',
                    fill: true,
                    tension: 0.4,
                    borderWidth: 2
                }
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { labels: { color: muted, usePointStyle: true, padding: 16 } }
            },
            scales: {
                x: { grid: { color: border }, ticks: { color: muted } },
                y: { grid: { color: border }, ticks: { color: muted } }
            }
        }
    });
};
