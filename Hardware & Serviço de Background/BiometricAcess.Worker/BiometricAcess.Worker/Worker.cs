using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IAnvizConnector _connector;

        public Worker(ILogger<Worker> logger, IAnvizConnector connector)
        {
            _logger = logger;
            _connector = connector;
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

                _logger.LogInformation("Conectado ao T50M. Iniciando polling de eventos...");

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var evento = _connector.BuscarNovoEvento();

                        if (evento != null)
                        {
                            _logger.LogInformation($"Evento recebido — Pessoa: {evento.PessoaID} | Tipo: {evento.TipoVerificacao} | Liberado: {evento.AcessoLiberado} | Hora: {evento.DataHora}");
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