using BiometricAcess.Worker;
using BiometricAcess.Worker.Services;
using BiometricAcess.Worker.Simulador;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();

builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();
builder.Services.AddSingleton<IAnvizService, AnvizServiceSimulador>();
builder.Services.AddSingleton<IEventProcessor, EventProcessorSimulador>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();