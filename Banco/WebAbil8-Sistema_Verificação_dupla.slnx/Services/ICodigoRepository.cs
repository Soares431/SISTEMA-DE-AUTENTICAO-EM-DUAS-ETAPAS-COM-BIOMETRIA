using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ICodigoRepository
    {
        CodigoDisponivel? BuscarDisponivel();              // retorna um código livre aleatório (100000-999999)
        CodigoDisponivel? BuscarPorCodigo(string codigo);
        CodigoDisponivel MarcarEmUso(string codigo, bool emUso, long? pessoaId = null);
        CodigoDisponivel Liberar(string codigo);           // marca EmUso=false e zera pessoaId (volta ao pool)
        List<CodigoDisponivel> ListarTodos();
        int ContarDisponiveis();
    }
}
