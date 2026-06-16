window.themeManager = {

    setTheme: function(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
    },

    getTheme: function() {
        return localStorage.getItem('theme') || (window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    },

    initTheme: function() {
        const theme = this.getTheme();
        document.documentElement.setAttribute('data-theme', theme);
    }
};

window.themeManager.initTheme();

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

window._hlsInstances = window._hlsInstances || {};

window.iniciarHls = function(videoElementId, urlHls) {
    var video = document.getElementById(videoElementId);
    if (!video) return false;

    window.pararHls(videoElementId);

    if (video.canPlayType('application/vnd.apple.mpegurl')) {
        video.src = urlHls;
        var onMeta = function() {
            try {
                var seekable = video.seekable;
                if (seekable.length > 0) video.currentTime = seekable.end(seekable.length - 1);
            } catch (e) {}
            video.play().catch(function(){});
        };
        video.addEventListener('loadedmetadata', onMeta, { once: true });
        return true;
    }

    if (typeof Hls === 'undefined' || !Hls.isSupported()) {
        console.error('hls.js não carregado ou não suportado neste browser');
        return false;
    }

    var hls = new Hls({
        lowLatencyMode: true,
        liveSyncDurationCount: 1,
        liveMaxLatencyDurationCount: 3,
        maxLiveSyncPlaybackRate: 1.5,
        backBufferLength: 0
    });
    hls.loadSource(urlHls);
    hls.attachMedia(video);
    hls.on(Hls.Events.MANIFEST_PARSED, function() {

        try {
            if (hls.liveSyncPosition) video.currentTime = hls.liveSyncPosition;
        } catch (e) {}
        video.play().catch(function(){});
    });
    hls.on(Hls.Events.ERROR, function(event, data) {
        console.warn('HLS error:', data.type, data.details, 'fatal=' + data.fatal);
        if (data.fatal) {
            try { hls.destroy(); } catch (e) {}
            delete window._hlsInstances[videoElementId];
        }
    });
    window._hlsInstances[videoElementId] = hls;
    return true;
};

window.pararHls = function(videoElementId) {
    var video = document.getElementById(videoElementId);
    if (video) {
        try { video.pause(); } catch(e) {}
        video.removeAttribute('src');
        video.load();
    }
    var hls = window._hlsInstances[videoElementId];
    if (hls) {
        try { hls.destroy(); } catch(e) {}
        delete window._hlsInstances[videoElementId];
    }
};

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

window.maskCpf = function(el) {
    var v = (el.value || '').replace(/\D/g, '').slice(0, 11);
    var out = v;
    if (v.length > 9)      out = v.substring(0,3) + '.' + v.substring(3,6) + '.' + v.substring(6,9) + '-' + v.substring(9);
    else if (v.length > 6) out = v.substring(0,3) + '.' + v.substring(3,6) + '.' + v.substring(6);
    else if (v.length > 3) out = v.substring(0,3) + '.' + v.substring(3);
    el.value = out;
};

window.maskTelefone = function(el) {
    var v = (el.value || '').replace(/\D/g, '').slice(0, 11);
    var out = v;
    if (v.length > 10)     out = '(' + v.substring(0,2) + ') ' + v.substring(2,7) + '-' + v.substring(7,11);
    else if (v.length > 6) out = '(' + v.substring(0,2) + ') ' + v.substring(2,6) + '-' + v.substring(6);
    else if (v.length > 2) out = '(' + v.substring(0,2) + ') ' + v.substring(2);
    else if (v.length > 0) out = '(' + v;
    el.value = out;
};

window.maskSomenteDigitos = function(el, max) {
    el.value = (el.value || '').replace(/\D/g, '').slice(0, max || 11);
};

window.maskTextoSeguro = function(el, max) {
    el.value = (el.value || '').replace(/[^a-zA-ZÀ-ÿ\s'\-]/g, '').slice(0, max || 100);
};

// IP: 999.999.999.999 — bloqueia letras/símbolos, permite dígitos e pontos,
// máx 4 octetos de 3 dígitos cada. Aceita "1.168.0.1" e "192.168.0.218" igual.
window.maskIp = function(el) {
    var raw   = (el.value || '').replace(/[^\d.]/g, '').replace(/\.+/g, '.');
    var parts = raw.split('.').slice(0, 4).map(function(p) { return p.slice(0, 3); });
    el.value  = parts.join('.');
};

// Endereço T50: IP do dispositivo na rede (formato 192.168.0.x).
window.maskEnderecoT50 = function(el) {
    var v = (el.value || '');
    var raw   = v.replace(/[^\d.]/g, '').replace(/\.+/g, '.');
    var parts = raw.split('.').slice(0, 4).map(function(p) { return p.slice(0, 3); });
    el.value  = parts.join('.');
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

