namespace BiometricAcess.Worker.Services
{
    // Sincroniza diariamente a hora do dispositivo conectado (T50M) com o servidor.
    // doc_tecnica §8.3 + recomendação da seção 5.8 do ENTREGA_CLIENTE.md.
    //
    // Dispara diariamente às 03:30 UTC (após os jobs do Int1 às 03:00, fora do horário de
    // operação do quartel). Faz primeira sincronização 60s após o startup pra validar
    // que a comunicação funciona.
    //
    // Limitação: como o Worker mantém UMA conexão ativa por instância (IAnvizService
    // Singleton), só o dispositivo conectado é sincronizado. Para multi-T50, rodar uma
    // instância do Worker por dispositivo.
    public class TimeSyncWorker : BackgroundService
    {
        private readonly ILogger<TimeSyncWorker> _logger;
        private readonly IAnvizService _anvizService;
        private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(60);
        private const int HoraUtc = 3;
        private const int MinutoUtc = 30;

        public TimeSyncWorker(ILogger<TimeSyncWorker> logger, IAnvizService anvizService)
        {
            _logger = logger;
            _anvizService = anvizService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Primeira sincronização logo após o startup (60s pra dar tempo da conexão estabilizar)
            try { await Task.Delay(InitialDelay, stoppingToken); }
            catch (TaskCanceledException) { return; }

            Sincronizar();

            while (!stoppingToken.IsCancellationRequested)
            {
                var proximaExecucao = ProximaJanela03h30Utc(DateTime.UtcNow);
                var atraso = proximaExecucao - DateTime.UtcNow;
                _logger.LogInformation("Próxima sincronização de hora em {proximaExec:yyyy-MM-dd HH:mm} UTC ({atraso})",
                    proximaExecucao, atraso);

                try { await Task.Delay(atraso, stoppingToken); }
                catch (TaskCanceledException) { return; }

                Sincronizar();
            }
        }

        private void Sincronizar()
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
        }

        // Próximo 03:30 UTC a partir de `agora`. Se já passou hoje, retorna 03:30 UTC de amanhã.
        internal static DateTime ProximaJanela03h30Utc(DateTime agora)
        {
            var alvo = new DateTime(agora.Year, agora.Month, agora.Day, HoraUtc, MinutoUtc, 0, DateTimeKind.Utc);
            if (alvo <= agora) alvo = alvo.AddDays(1);
            return alvo;
        }
    }
}
