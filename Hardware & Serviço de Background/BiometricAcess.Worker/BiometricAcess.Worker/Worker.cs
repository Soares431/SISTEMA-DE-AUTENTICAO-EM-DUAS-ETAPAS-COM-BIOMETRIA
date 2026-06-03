using BiometricAcess.Worker.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAnvizConnector _connector;
        private readonly IEventProcessor _eventProcessor;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(ILogger<Worker> logger, IAnvizConnector connector, IEventProcessor eventProcessor, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _connector = connector;
            _eventProcessor = eventProcessor;
            _scopeFactory = scopeFactory;
        }

        private void RegistrarHeartbeat()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IDispositivoT50Repository>();
                repo.RegistrarHeartbeat(_connector.EnderecoIdentificador);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Falha ao registrar heartbeat: {msg}", ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bool conectado = _connector.Conectar();

                if (!conectado)
                {
                    _logger.LogWarning("Falha ao conectar ao T50M. Tentando novamente em 10 segundos...");
                    await Task.Delay(10000, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Conectado ao T50M. Buscando eventos armazenados...");
                RegistrarHeartbeat();

                var eventosArmazenados = _connector.BuscarEventosArmazenados();
                foreach (var eventoArmazenado in eventosArmazenados)
                {
                    await _eventProcessor.Processar(eventoArmazenado);
                }

                _logger.LogInformation("Iniciando polling de eventos em tempo real...");

                try
                {
                    // Heartbeat a cada N ciclos do polling — evita escrita constante no banco,
                    // mas mantém status online enquanto conexão está viva mesmo sem eventos.
                    int ciclosAteProximoHeartbeat = 0;
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var evento = _connector.BuscarNovoEvento();

                        if (evento != null)
                        {
                            await _eventProcessor.Processar(evento);
                        }

                        if (--ciclosAteProximoHeartbeat <= 0)
                        {
                            RegistrarHeartbeat();
                            ciclosAteProximoHeartbeat = 30; // 30 * 2s = 60s
                        }

                        await Task.Delay(2000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Conexão perdida: {ex.Message}. Reconectando...");
                    _connector.Desconectar();
                }
            }
        }
    }
}