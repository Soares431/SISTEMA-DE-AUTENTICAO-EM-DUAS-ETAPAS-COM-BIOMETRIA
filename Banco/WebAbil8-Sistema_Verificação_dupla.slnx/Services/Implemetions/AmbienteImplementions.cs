using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class AmbienteImplementions : IAmbienteRepository
    {
        private readonly AppDbContext _context;

        public AmbienteImplementions(AppDbContext context)
        {
            _context = context;
        }

        public Ambiente Adicionar(Ambiente ambiente)
        {
            _context.Ambientes.Add(ambiente);
            _context.SaveChanges();
            return ambiente;
        }

        public Ambiente Atualizar(Ambiente ambiente)
        {
            var existing = _context.Ambientes.Find(ambiente.Id);
            if (existing == null) throw new ArgumentNullException("Ambiente não encontrado");
            _context.Entry(existing).CurrentValues.SetValues(ambiente);
            _context.SaveChanges();
            return ambiente;
        }

        public Ambiente BuscarPorId(int id)
        {
            return _context.Ambientes.Find(id);
        }

        public List<Ambiente> ListarTodos()
        {

            return _context.Ambientes.Where(a => !a.Excluido).ToList();
        }

        public List<Ambiente> ListarTodosIncluindoExcluidos()
        {

            return _context.Ambientes.ToList();
        }

        public void Remover(int id)
        {
            var existing = _context.Ambientes.Find(id);
            if (existing == null) throw new ArgumentNullException("Ambiente não encontrado");

            existing.Excluido = true;
            existing.DataExclusao = DateTime.UtcNow;
            _context.SaveChanges();
        }

    }
}

