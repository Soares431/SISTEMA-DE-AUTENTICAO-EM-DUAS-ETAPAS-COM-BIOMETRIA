using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IAmbienteRepository
    {
        Ambiente Adicionar(Ambiente ambiente);
        Ambiente BuscarPorId(int id);

        List<Ambiente> ListarTodos();
        List<Ambiente> ListarTodosIncluindoExcluidos();
        Ambiente Atualizar(Ambiente ambiente);

        void Remover(int id);
    }
}

