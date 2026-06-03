namespace BiometricAcess.Worker.Services
{
    // Sincroniza hora do dispositivo conectado (T50M / Arduino) com o servidor.
    // doc_tecnica §8.3 + recomendação da seção 5.8 do ENTREGA_CLIENTE.md.
    //
    // Roda em paralelo ao Worker principal. Espera 60s na primeira execução para dar tempo
    // do conector estabelecer link, depois sincroniza a cada 24h.
    //
    // Limitação atual: como o Worker mantém UMA conexão ativa por instância (IAnvizService
    // Singleton), só o dispositivo conectado é sincronizado. Para multi-T50, instalar o
    // Worker como serviço separado por dispositivo.
    public class TimeSyncWorker : BackgroundService
    {
        private readonly ILogger<TimeSyncWorker> _logger;
        private readonly IAnvizService _anvizService;
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        public TimeSyncWorker(ILogger<TimeSyncWorker> logger, IAnvizService anvizService)
        {
            _logger = logger;
            _anvizService = anvizService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await Task.Delay(InitialDelay, stoppingToken);
            }
            catch (TaskCanceledException) { return; }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var ok = _anvizService.SincronizarHora();
                    if (ok)
                        _logger.LogInformation("Hora do dispositivo sincronizada com sucesso.");
                    else
                        _logger.LogWarning("Falha ao sincronizar hora do dispositivo.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro inesperado ao sincronizar hora.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (TaskCanceledException) { return; }
            }
        }
    }
}
