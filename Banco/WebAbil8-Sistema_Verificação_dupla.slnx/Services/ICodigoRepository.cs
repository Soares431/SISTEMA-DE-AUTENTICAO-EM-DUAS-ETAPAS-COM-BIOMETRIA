using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ICodigoRepository
    {
        CodigoDisponivel? BuscarDisponivel();
        CodigoDisponivel? BuscarPorCodigo(string codigo);
        CodigoDisponivel MarcarEmUso(string codigo, bool emUso, long? pessoaId = null);
        CodigoDisponivel Liberar(string codigo);
        List<CodigoDisponivel> ListarTodos();
        int ContarDisponiveis();
    }
}

