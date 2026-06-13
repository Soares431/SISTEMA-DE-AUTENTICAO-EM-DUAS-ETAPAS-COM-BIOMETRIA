using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IAdministradorRepository
    {
        Administrador Adicionar(Administrador admin);
        Administrador? BuscarPorId(int id);
        Administrador? BuscarPorLogin(string login);
        List<Administrador> ListarTodos();
        Administrador Atualizar(Administrador admin);
        bool LoginExiste(string login, int? ignorarId = null);
    }
}

