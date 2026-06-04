// Jobs/LimparDadosExpiradosJob.cs
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

            // Deleta o arquivo MP4 de cada tentativa expirada antes de remover do banco.
            // Sem isto, os MP4 ficariam órfãos em disco consumindo espaço.
            int arquivosRemovidos = 0;
            foreach (var t in tentativasExpiradas)
            {
                if (!string.IsNullOrEmpty(t.GravacaoPath))
                {
                    try
                    {
                        if (File.Exists(t.GravacaoPath))
                        {
                            File.Delete(t.GravacaoPath);
                            arquivosRemovidos++;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Não bloqueia a limpeza do banco se um arquivo falhar (pode estar em uso)
                        _logger.LogWarning(ex, "Falha ao deletar gravação {path} da tentativa {id}", t.GravacaoPath, t.Id);
                    }
                }
            }

            _context.TentativasAcesso.RemoveRange(tentativasExpiradas);
            _logger.LogInformation("{count} tentativas expiradas removidas ({arquivos} arquivos MP4 deletados).",
                tentativasExpiradas.Count, arquivosRemovidos);

            var logsExpirados = _context.LogsAdmin
                .Where(l => l.DataExpiracao != null && l.DataExpiracao < agora)
                .ToList();
            _context.LogsAdmin.RemoveRange(logsExpirados);
            _logger.LogInformation("{count} logs expirados removidos.", logsExpirados.Count);

            _context.SaveChanges();

            // Purga física de ambientes soft-deletados que já não têm tentativas vivas.
            // Mantém o histórico do Histórico até o último registro expirar.
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