using BiometricAcess.Worker;
using BiometricAcess.Worker.Services;
using BiometricAcess.Worker.Simulador;

var builder = Host.CreateApplicationBuilder(args);

// Registra o simulador como implementação da interface
// Quando tiver o hardware, troca AnvizConnectorSimulador por AnvizConnector
builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();