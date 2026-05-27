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

    // Destroi instância anterior para evitar erro "Canvas is already in use" ao
    // navegar para fora e voltar ao Dashboard
    const existing = Chart.getChart(canvas);
    if (existing) existing.destroy();

    const ctx = canvas.getContext('2d');

    // Lê as cores do design system (variáveis CSS) para manter consistência com o tema
    const style = getComputedStyle(document.documentElement);
    const primary = style.getPropertyValue('--chart-1').trim() || '#34d399';       // Verde - acessos permitidos
    const destructive = style.getPropertyValue('--chart-3').trim() || '#ef4444';   // Vermelho - acessos negados
    const border = style.getPropertyValue('--border').trim() || '#2d3a52';         // Cor das linhas do grid
    const muted = style.getPropertyValue('--muted-foreground').trim() || '#8b95a8'; // Cor dos rótulos

    // Intervalo visível: 06h às 22h — índices 6 a 22 (inclusive)
    // Cobre expediente militar e períodos noturnos, sem ocultar eventos à tarde/noite
    const labels = ['06:00','07:00','08:00','09:00','10:00','11:00','12:00',
                    '13:00','14:00','15:00','16:00','17:00','18:00','19:00',
                    '20:00','21:00','22:00'];
    const permitidos = (permitidosData || new Array(24).fill(0)).slice(6, 23);
    const negados    = (negadosData    || new Array(24).fill(0)).slice(6, 23);

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
