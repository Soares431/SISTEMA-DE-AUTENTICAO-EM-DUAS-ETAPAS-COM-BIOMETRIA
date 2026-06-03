using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class CodigoImplemetions : ICodigoRepository
    {
        private readonly AppDbContext _context;

        public CodigoImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public CodigoDisponivel? BuscarDisponivel()
        {
            // Range 100000-999999, sem zeros à esquerda — mesma lógica de SenhaImplemetions
            return _context.CodigosDisponiveis
                .Where(c => !c.EmUso && c.PessoaId == null)
                .AsEnumerable()
                .Where(c => int.TryParse(c.Codigo, out var n) && n >= 100000 && n <= 999999)
                .OrderBy(c => Guid.NewGuid())
                .FirstOrDefault();
        }

        public CodigoDisponivel? BuscarPorCodigo(string codigo)
        {
            return _context.CodigosDisponiveis.Find(codigo);
        }

        public CodigoDisponivel MarcarEmUso(string codigo, bool emUso, long? pessoaId = null)
        {
            var existing = _context.CodigosDisponiveis.Find(codigo);
            if (existing == null) throw new ArgumentNullException(nameof(codigo), "Código não encontrado");
            existing.EmUso = emUso;
            existing.PessoaId = emUso ? pessoaId : null;
            _context.SaveChanges();
            return existing;
        }

        public CodigoDisponivel Liberar(string codigo)
        {
            return MarcarEmUso(codigo, false, null);
        }

        public List<CodigoDisponivel> ListarTodos()
        {
            return _context.CodigosDisponiveis.ToList();
        }

        public int ContarDisponiveis()
        {
            return _context.CodigosDisponiveis.Count(c => !c.EmUso);
        }
    }
}
