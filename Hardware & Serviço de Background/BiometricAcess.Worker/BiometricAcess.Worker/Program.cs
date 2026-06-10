using BiometricAcess.Worker;
using BiometricAcess.Worker.Services;
using BiometricAcess.Worker.Simulador;
using BiometricAcess.Worker.HardwareNosso;
using BiometricAcess.Worker.HardwareNosso.Simulador;
using InfraestruturaBloco1.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();

// ═══════════════════════════════════════════════════════════════
// Banco compartilhado com Int1
// ═══════════════════════════════════════════════════════════════
var dbPath = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", "..",
    "Banco", "WebAbil8-Sistema_Verificação_dupla.slnx", "banco.db"));
Console.WriteLine($"[INT2 DB] {dbPath}");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IPessoaRepository, PessoaImplemetions>();
builder.Services.AddScoped<IAmbienteRepository, AmbienteImplementions>();
builder.Services.AddScoped<IAmbientePessoaRepository, AmbientePessoaImplemetions>();
builder.Services.AddScoped<IDispositivoT50Repository, DispositivoT50Implemetions>();
builder.Services.AddScoped<ITentativaAcessoRepository, TentativaAcessoImplemetions>();
builder.Services.AddScoped<IConfiguracaoRepository, ConfiguracaoImplemetions>();
builder.Services.AddScoped<ILogAdminRepository, LogAdminImplemetions>();
builder.Services.AddScoped<ICameraRepository, CameraImplemetions>();
builder.Services.AddScoped<IAmbienteT50Repository, AmbienteT50Implemetions>();
builder.Services.AddScoped<IPessoaT50Repository, PessoaT50Implemetions>();
builder.Services.AddScoped<IT50PendenciaRepository, T50PendenciaImplemetions>();

// CameraService — consumido pelos EventProcessors reais (Arduino + T50M) via IServiceScopeFactory
// para associar gravações ONVIF a tentativas de acesso liberado (§5.11 doc técnica).
builder.Services.AddScoped<CameraService>();

// ═══════════════════════════════════════════════════════════════
// OPÇÃO 1 — Simulador falso (sem banco, apenas console)
// ═══════════════════════════════════════════════════════════════
//builder.Services.AddSingleton<IEventProcessor, EventProcessorSimulador>();

// ═══════════════════════════════════════════════════════════════
// OPÇÃO 1B — Simulador com banco real (padrão para demonstração)
// Grava TentativasAcesso no SQLite — o painel exibe os dados em tempo real
// Requer pelo menos um Ambiente cadastrado no painel
// ═══════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
builder.Services.AddSingleton<IEventProcessor, EventProcessorSimuladorBanco>();

// ═══════════════════════════════════════════════════════════════
// ═══════════════════════════════════════════════════════════════
// OPÇÃO 2 — Nosso Arduino (hardware físico)
// Antes de ativar: ajuste a porta COM no ArduinoConnector
// ═══════════════════════════════════════════════════════════════

//var arduinoConnector = new ArduinoConnector(porta: "COM3"); // ← ajuste a porta
//builder.Services.AddSingleton<IAnvizConnector>(arduinoConnector);
//builder.Services.AddSingleton<IAnvizService>(new ArduinoService(arduinoConnector));
//builder.Services.AddSingleton<IAnvizArduinoService>(new ArduinoServiceExtras(arduinoConnector));

// OPÇÃO 2A — banco vazio / dados mockados
//builder.Services.AddSingleton<IEventProcessor, EventProcessorArduinoSimulador>();

// OPÇÃO 2B — banco real
//builder.Services.AddSingleton<IEventProcessor, EventProcessorArduino>();

// ═══════════════════════════════════════════════════════════════

// OPÇÃO 3 — T50M real (hardware Anviz)
//builder.Services.AddSingleton<IAnvizConnector, AnvizConnector>();
//builder.Services.AddSingleton<IAnvizService, AnvizService>();
//builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

// ═══════════════════════════════════════════

builder.Services.AddHostedService<Worker>();

// Sincroniza hora do T50M com o servidor diariamente (no-op no simulador).
builder.Services.AddHostedService<TimeSyncWorker>();

// Consome a fila T50Pendencia (cadastros/remoções enfileiradas pelo Frontend) e executa
// via Anviz SDK no hardware. §5.2 doc técnica.
builder.Services.AddHostedService<SincronizadorT50Worker>();

// Drena fila de slots a apagar no AS608 (Pessoa.SlotAs608ParaApagar). Idle quando
// IAnvizArduinoService não está registrado (modos simulador/T50M).
builder.Services.AddHostedService<SincronizadorAs608Worker>();

var host = builder.Build();

host.Run();
