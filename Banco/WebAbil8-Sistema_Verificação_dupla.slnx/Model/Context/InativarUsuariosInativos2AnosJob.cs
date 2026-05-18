using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Jobs
{
    public class InativarUsuariosInativos2AnosJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InativarUsuariosInativos2AnosJob> _logger;

        public InativarUsuariosInativos2AnosJob(
            IServiceProvider serviceProvider,
            ILogger<InativarUsuariosInativos2AnosJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Executando job: InativarUsuariosInativos2Anos");

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var doisAnosAtras = DateTime.UtcNow.AddYears(-2);

                var usuarios = db.Pessoas
                    .Where(p => p.Status == "ativo"
                        && p.dataUltimoAcesso < doisAnosAtras)
                    .ToList();

                foreach (var usuario in usuarios)
                {
                    usuario.Status = "inativo";
                    _logger.LogInformation("Usuário {id} inativado por inatividade.", usuario.Id);
                }

                await db.SaveChangesAsync();

                // Roda uma vez por dia
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}