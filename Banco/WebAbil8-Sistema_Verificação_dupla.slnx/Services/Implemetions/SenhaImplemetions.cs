using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class SenhaImplemetions : ISenhaRepository
    {
        private readonly AppDbContext _context;

        public SenhaImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public SenhaDisponivel Adicionar(SenhaDisponivel senha)
        {
            _context.SenhasDisponiveis.Add(senha);
            _context.SaveChanges();
            return senha;
        }

        public SenhaDisponivel Atualizar(SenhaDisponivel senha)
        {
            var existing = _context.SenhasDisponiveis.Find(senha.Senha);
            if (existing == null) throw new ArgumentNullException("Senha não encontrada");
            _context.Entry(existing).CurrentValues.SetValues(senha);
            _context.SaveChanges();
            return senha;
        }

        public SenhaDisponivel BuscarDisponivel(string id)
        {
            return _context.SenhasDisponiveis.Find(id);
        }

        public SenhaDisponivel BuscarDisponivel()
        {
            // Garante range 100000 a 999999 — sem zeros à esquerda
            return _context.SenhasDisponiveis
                .Where(s => !s.EmUso
                    && s.PessoaId == null
                    && string.Compare(s.Senha, "100000") >= 0
                    && string.Compare(s.Senha, "999999") <= 0)
                .OrderBy(s => Guid.NewGuid()) // aleatório
                .FirstOrDefault();
        }


        public SenhaDisponivel Liberar(string senha)
        {
            return MarcarEmUso(senha, false);
        }

        public List<SenhaDisponivel> ListarTodos()
        {
            return _context.SenhasDisponiveis.ToList();
        }

        public SenhaDisponivel MarcarEmUso(string senha, bool emUso)
        {
            var existing = _context.SenhasDisponiveis.Find(senha);
            if (existing == null) throw new ArgumentNullException("Senha não encontrada");
            existing.EmUso = emUso;
            _context.SaveChanges();
            return existing;
        }

        public void Remover(string id)
        {
            var existing = _context.SenhasDisponiveis.Find(id);
            if (existing == null) throw new ArgumentNullException("Senha não encontrada");
            _context.SenhasDisponiveis.Remove(existing);
            _context.SaveChanges();
        }
    }
}
