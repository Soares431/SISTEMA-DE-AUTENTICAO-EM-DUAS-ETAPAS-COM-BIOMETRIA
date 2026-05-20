using BiometricAcess.Worker;
using BiometricAcess.Worker.Services;
using BiometricAcess.Worker.Simulador;
using BiometricAcess.Worker.HardwareNosso;
using BiometricAcess.Worker.HardwareNosso.Simulador;
using InfraestruturaBloco1.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();

// ═══════════════════════════════════════════════════════════════
// Serviços compartilhados
// ═══════════════════════════════════════════════════════════════
builder.Services.AddScoped<CameraService>(sp =>
    new CameraService(
        sp.GetRequiredService<ILogAdminRepository>(),
        Environment.GetEnvironmentVariable("CAMERA_BASE_PATH") ?? "cameras"
    ));

// ═══════════════════════════════════════════════════════════════
// OPÇÃO 1 — Simulador falso (padrão, sem hardware)
// ═══════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
builder.Services.AddSingleton<IEventProcessor, EventProcessorSimulador>();

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

// ═══════════════════════════════════════════════

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();