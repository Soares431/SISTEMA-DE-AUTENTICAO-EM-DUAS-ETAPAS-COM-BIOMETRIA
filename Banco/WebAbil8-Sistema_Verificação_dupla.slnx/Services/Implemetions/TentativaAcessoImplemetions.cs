// TentativaAcessoImplemetions.cs
using Microsoft.Data.Sqlite;
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

        // Update direto via SqliteConnection separada. Tentamos antes via Find+SetValues
        // (no-op por causa do tracking) e ExecuteSqlRaw do EF Core (também não persistia
        // por algum motivo — provavelmente o DbContext do scope ainda mantém o snapshot
        // antigo e algo na escrita não chega ao SQLite).
        //
        // Abrir uma conexão NOVA garante:
        //   1. Bypass total do change tracker do EF
        //   2. Commit imediato (SqliteConnection padrão = autocommit)
        //   3. Visibilidade imediata pra outros leitores (incluindo a API Int1)
        //
        // Se der exception, propaga para o caller logar — antes ficava silencioso.
        public int AtualizarGravacaoPath(int tentativaId, string gravacaoPath)
        {
            var connString = _context.Database.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connString))
                throw new InvalidOperationException("ConnectionString não disponível no DbContext.");

            using var conn = new SqliteConnection(connString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE tentativaAcesso SET gravacaoPath = $path WHERE id = $id";
            cmd.Parameters.AddWithValue("$path", gravacaoPath);
            cmd.Parameters.AddWithValue("$id", tentativaId);
            return cmd.ExecuteNonQuery();
        }
    }
}