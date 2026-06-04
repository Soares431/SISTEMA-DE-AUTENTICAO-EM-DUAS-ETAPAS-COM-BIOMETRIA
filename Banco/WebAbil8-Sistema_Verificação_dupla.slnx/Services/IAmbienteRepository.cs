using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IAmbienteRepository
    {
        Ambiente Adicionar(Ambiente ambiente);
        Ambiente BuscarPorId(int id);
        // ListarTodos retorna apenas ambientes ativos (excluido = false). Para incluir
        // ambientes excluídos (necessário no Histórico para exibir nome do ambiente
        // de tentativas antigas), use ListarTodosIncluindoExcluidos.
        List<Ambiente> ListarTodos();
        List<Ambiente> ListarTodosIncluindoExcluidos();
        Ambiente Atualizar(Ambiente ambiente);
        // Soft-delete: marca como excluido = true, dataExclusao = agora.
        // Limpeza física ocorre no job de retenção junto com as tentativas.
        void Remover(int id);
    }
}
