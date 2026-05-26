using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class LogAdminImplemetions : ILogAdminRepository
    {
        private readonly AppDbContext _context;

        public LogAdminImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public LogAdmin Adicionar(LogAdmin log)
        {
            _context.LogsAdmin.Add(log);
            _context.SaveChanges();
            return log;
        }

        public LogAdmin Atualizar(LogAdmin log)
        {
            var existing = _context.LogsAdmin.Find(log.Id);
            if (existing == null) throw new ArgumentNullException("Log não encontrado");
            _context.Entry(existing).CurrentValues.SetValues(log);
            _context.SaveChanges();
            return log;
        }

        public LogAdmin BuscarPorId(long id)
        {
            return _context.LogsAdmin.Find(id);
        }

        public List<LogAdmin> ListarComFiltros(int? adminId, string acao, string entidadeAfetada, DateTime? dataInicio, DateTime? dataFim)
        {
            var query = _context.LogsAdmin.AsQueryable();

            if (adminId.HasValue)
                query = query.Where(l => l.AdminId == adminId);
            if (!string.IsNullOrEmpty(acao))
                query = query.Where(l => l.Acao.Contains(acao));
            if (!string.IsNullOrEmpty(entidadeAfetada))
                query = query.Where(l => l.EntidadeAfetada == entidadeAfetada);
            if (dataInicio.HasValue)
                query = query.Where(l => l.DataHora >= dataInicio);
            if (dataFim.HasValue)
                query = query.Where(l => l.DataHora <= dataFim);

            return query.ToList();
        }

        public List<LogAdmin> ListarTodos()
        {
            return _context.LogsAdmin.Include(l => l.Administrador).ToList();
        }

        public LogAdmin Registrar(int adminId, string acao, string entidadeAfetada, int? entidadeId)
        {
            var retencaoDias = _context.Configuracoes.FirstOrDefault()?.RetencaoLogsDias ?? 180;
            var log = new LogAdmin
            {
                AdminId = adminId,
                Acao = acao,
                EntidadeAfetada = entidadeAfetada,
                EntidadeId = entidadeId,
                DataHora = DateTime.UtcNow,
                DataExpiracao = DateTime.UtcNow.AddDays(retencaoDias)
            };
            _context.LogsAdmin.Add(log);
            _context.SaveChanges();
            return log;
        }

        public void Remover(long id)
        {
            var existing = _context.LogsAdmin.Find(id);
            if (existing == null) throw new ArgumentNullException("Log não encontrado");
            _context.LogsAdmin.Remove(existing);
            _context.SaveChanges();
        }
    }
}