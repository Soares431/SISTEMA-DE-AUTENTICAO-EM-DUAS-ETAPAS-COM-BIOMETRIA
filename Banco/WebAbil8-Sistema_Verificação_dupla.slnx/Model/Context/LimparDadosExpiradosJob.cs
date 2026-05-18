using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Jobs
{
    public class LimparDadosExpiradosJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LimparDadosExpiradosJob> _logger;

        public LimparDadosExpiradosJob(
            IServiceProvider serviceProvider,
            ILogger<LimparDadosExpiradosJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Executando job: LimparDadosExpirados");

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var agora = DateTime.UtcNow;

                // Remove TentativasAcesso expiradas
                var tentativasExpiradas = db.TentativasAcesso
                    .Where(t => t.DataExpiracao != null && t.DataExpiracao < agora)
                    .ToList();
                db.TentativasAcesso.RemoveRange(tentativasExpiradas);
                _logger.LogInformation("{count} tentativas expiradas removidas.", tentativasExpiradas.Count);

                // Remove Logs expirados
                var logsExpirados = db.LogsAdmin
                    .Where(l => l.DataExpiracao != null && l.DataExpiracao < agora)
                    .ToList();
                db.LogsAdmin.RemoveRange(logsExpirados);
                _logger.LogInformation("{count} logs expirados removidos.", logsExpirados.Count);

                await db.SaveChangesAsync();

                // Roda uma vez por dia
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}