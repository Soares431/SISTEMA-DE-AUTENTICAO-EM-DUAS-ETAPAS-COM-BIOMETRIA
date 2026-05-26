using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class AdministradorImplemetions : IAdministradorRepository
    {
        private readonly AppDbContext _context;

        public AdministradorImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public Administrador Adicionar(Administrador admin)
        {
            _context.Administradores.Add(admin);
            _context.SaveChanges();
            return admin;
        }

        public Administrador? BuscarPorId(int id)
        {
            return _context.Administradores.Find(id);
        }

        public Administrador? BuscarPorLogin(string login)
        {
            return _context.Administradores.FirstOrDefault(a => a.Login == login);
        }

        public List<Administrador> ListarTodos()
        {
            return _context.Administradores.OrderBy(a => a.NomeCompleto).ToList();
        }

        public Administrador Atualizar(Administrador admin)
        {
            var existing = _context.Administradores.Find(admin.Id);
            if (existing == null) throw new ArgumentException("Administrador não encontrado");
            _context.Entry(existing).CurrentValues.SetValues(admin);
            _context.SaveChanges();
            return admin;
        }

        public bool LoginExiste(string login, int? ignorarId = null)
        {
            return _context.Administradores.Any(a =>
                a.Login == login && (ignorarId == null || a.Id != ignorarId));
        }
    }
}
