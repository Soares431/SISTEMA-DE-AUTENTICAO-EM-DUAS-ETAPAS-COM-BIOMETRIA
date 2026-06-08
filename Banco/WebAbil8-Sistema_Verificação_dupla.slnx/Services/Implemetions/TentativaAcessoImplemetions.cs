// TentativaAcessoImplemetions.cs
using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class TentativaAcessoImplemetions : ITentativaAcessoRepository
    {
        private readonly AppDbContext _context;

        public TentativaAcessoImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public TentativaAcesso Adicionar(TentativaAcesso tentativa)
        {
            _context.TentativasAcesso.Add(tentativa);
            _context.SaveChanges();
            return tentativa;
        }

        public TentativaAcesso Atualizar(TentativaAcesso tentativa)
        {
            var existing = _context.TentativasAcesso.Find(tentativa.Id);
            if (existing == null) throw new ArgumentNullException("Tentativa não encontrada");
            _context.Entry(existing).CurrentValues.SetValues(tentativa);
            _context.SaveChanges();
            return tentativa;
        }

        public TentativaAcesso BuscarPorId(int id)
        {
            return _context.TentativasAcesso.Find(id);
        }

        public List<TentativaAcesso> ListarComFiltros(long? pessoaId, int? ambienteId, bool? acessoLiberado, DateTime? dataInicio, DateTime? dataFim)
        {
            var query = _context.TentativasAcesso
                .Include(t => t.Pessoa)
                .Include(t => t.Ambiente)
                .AsQueryable();

            if (pessoaId.HasValue)
                query = query.Where(t => t.PessoaId == pessoaId);
            if (ambienteId.HasValue)
                query = query.Where(t => t.AmbienteId == ambienteId);
            if (acessoLiberado.HasValue)
                query = query.Where(t => t.AcessoLiberado == acessoLiberado);
            if (dataInicio.HasValue)
                query = query.Where(t => t.DataHora >= dataInicio);
            if (dataFim.HasValue)
                query = query.Where(t => t.DataHora <= dataFim);

            return query.ToList();
        }

        public List<TentativaAcesso> ListarPorAmbiente(int ambienteId)
        {
            return _context.TentativasAcesso
                .Include(t => t.Pessoa)
                .Include(t => t.Ambiente)
                .Where(t => t.AmbienteId == ambienteId)
                .ToList();
        }

        public List<TentativaAcesso> ListarPorPessoa(long pessoaId)
        {
            return _context.TentativasAcesso
                .Include(t => t.Pessoa)
                .Include(t => t.Ambiente)
                .Where(t => t.PessoaId == pessoaId)
                .ToList();
        }

        public List<TentativaAcesso> ListarTodos()
        {
            return _context.TentativasAcesso
                .Include(t => t.Pessoa)
                .Include(t => t.Ambiente)
                .ToList();
        }

        public TentativaAcesso Registrar(TentativaAcesso tentativa)
        {
            tentativa.DataHora = DateTime.UtcNow;
            _context.TentativasAcesso.Add(tentativa);
            _context.SaveChanges();
            return tentativa;
        }

        public void Remover(int id)
        {
            var existing = _context.TentativasAcesso.Find(id); // ← cast para int
            if (existing == null) throw new ArgumentNullException("Tentativa não encontrada");
            _context.TentativasAcesso.Remove(existing);
            _context.SaveChanges();
        }

        // Update direto via SQL. O caminho via tracking (Find + SetValues) estava virando no-op
        // quando o EventProcessor mantinha a entity tracked durante todo o Processar — o EF
        // reconciliava current=tentativa contra snapshot e o UPDATE não saía.
        // Retorna número de linhas afetadas para o caller verificar.
        public int AtualizarGravacaoPath(int tentativaId, string gravacaoPath)
        {
            return _context.Database.ExecuteSqlRaw(
                "UPDATE tentativaAcesso SET gravacaoPath = {0} WHERE id = {1}",
                gravacaoPath, tentativaId);
        }
    }
}