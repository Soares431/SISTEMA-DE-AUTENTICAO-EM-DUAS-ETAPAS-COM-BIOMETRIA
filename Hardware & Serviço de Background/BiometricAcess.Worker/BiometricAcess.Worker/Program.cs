using BiometricAcess.Worker;
using BiometricAcess.Worker.Services;
using BiometricAcess.Worker.Simulador;
using BiometricAcess.Worker.HardwareNosso;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();

// ═══════════════════════════════════════════════
// TROCA AQUI — comentar uma linha e descomentar outra
// ═══════════════════════════════════════════════

// OPÇÃO 1 — Simulador falso (dados mockados)
builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
builder.Services.AddSingleton<IEventProcessor, EventProcessorSimulador>();

// OPÇÃO 2 — Nosso Arduino (hardware físico ou Wokwi)
//var arduinoConnector = new ArduinoConnector(porta: "COM3");
//builder.Services.AddSingleton<IAnvizConnector>(arduinoConnector);
//builder.Services.AddSingleton<IAnvizService>(new ArduinoService(arduinoConnector));
//builder.Services.AddSingleton<IAnvizArduinoService>(new ArduinoServiceExtras(arduinoConnector));
//builder.Services.AddSingleton<IEventProcessor, EventProcessorArduino>();

// OPÇÃO 3 — T50M real (hardware Anviz)
//builder.Services.AddSingleton<IAnvizConnector, AnvizConnector>();
//builder.Services.AddSingleton<IAnvizService, AnvizService>();
//builder.Services.AddSingleton<IEventProcessor, EventProcessor>();

// ═══════════════════════════════════════════════

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();