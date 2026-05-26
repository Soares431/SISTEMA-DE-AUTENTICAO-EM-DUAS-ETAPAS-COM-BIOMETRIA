using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAnvizConnector _connector;
        private readonly IEventProcessor _eventProcessor;

        public Worker(ILogger<Worker> logger, IAnvizConnector connector, IEventProcessor eventProcessor)
        {
            _logger = logger;
            _connector = connector;
            _eventProcessor = eventProcessor;
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

                var eventosArmazenados = _connector.BuscarEventosArmazenados();
                foreach (var eventoArmazenado in eventosArmazenados)
                {
                    await _eventProcessor.Processar(eventoArmazenado);
                }

                _logger.LogInformation("Iniciando polling de eventos em tempo real...");

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var evento = _connector.BuscarNovoEvento();

                        if (evento != null)
                        {
                            await _eventProcessor.Processar(evento);
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