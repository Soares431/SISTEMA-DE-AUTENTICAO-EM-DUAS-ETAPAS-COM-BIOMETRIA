using BiometricAcess.Worker;
using BiometricAcess.Worker.Services;
using BiometricAcess.Worker.Simulador;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IAnvizConnector, AnvizConnectorSimulador>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();