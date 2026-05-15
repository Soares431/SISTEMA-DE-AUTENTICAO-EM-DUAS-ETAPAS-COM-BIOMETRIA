using InfraestruturaBloco1.Data;
using InfraestruturaBloco1.Models;
using Microsoft.EntityFrameworkCore;

namespace InfraestruturaBloco1.Services
{
    public class AuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAsync(string admin, string acao, string entidade)
        {
            var log = new AuditLog
            {
                Admin = admin,
                Acao = acao,
                Entidade = entidade,
                DataHora = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<AuditLog>> ConsultarAsync(
            string? admin = null,
            string? acao = null,
            string? entidade = null,
            DateTime? dataInicio = null,
            DateTime? dataFim = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(admin))
                query = query.Where(l => l.Admin == admin);

            if (!string.IsNullOrEmpty(acao))
                query = query.Where(l => l.Acao == acao);

            if (!string.IsNullOrEmpty(entidade))
                query = query.Where(l => l.Entidade == entidade);

            if (dataInicio.HasValue)
                query = query.Where(l => l.DataHora >= dataInicio.Value);

            if (dataFim.HasValue)
                query = query.Where(l => l.DataHora <= dataFim.Value);

            return await query.ToListAsync();
        }
    }
}
