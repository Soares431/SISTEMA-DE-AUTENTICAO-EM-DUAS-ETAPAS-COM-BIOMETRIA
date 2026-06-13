using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Jobs
{
    public class LimparDadosExpiradosJob
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LimparDadosExpiradosJob> _logger;

        public LimparDadosExpiradosJob(
            AppDbContext context,
            ILogger<LimparDadosExpiradosJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void Executar()
        {
            _logger.LogInformation("Executando job: LimparDadosExpirados");

            var agora = DateTime.UtcNow;

            var tentativasExpiradas = _context.TentativasAcesso
                .Where(t => t.DataExpiracao != null && t.DataExpiracao < agora)
                .ToList();

            _context.TentativasAcesso.RemoveRange(tentativasExpiradas);
            _logger.LogInformation("{count} tentativas expiradas removidas.", tentativasExpiradas.Count);

            var logsExpirados = _context.LogsAdmin
                .Where(l => l.DataExpiracao != null && l.DataExpiracao < agora)
                .ToList();
            _context.LogsAdmin.RemoveRange(logsExpirados);
            _logger.LogInformation("{count} logs expirados removidos.", logsExpirados.Count);

            _context.SaveChanges();

            var ambientesPurgar = _context.Ambientes
                .Where(a => a.Excluido && !_context.TentativasAcesso.Any(t => t.AmbienteId == a.Id))
                .ToList();
            if (ambientesPurgar.Any())
            {
                _context.Ambientes.RemoveRange(ambientesPurgar);
                _context.SaveChanges();
                _logger.LogInformation("{count} ambientes excluídos purgados (sem histórico vivo).", ambientesPurgar.Count);
            }
        }
    }
}
