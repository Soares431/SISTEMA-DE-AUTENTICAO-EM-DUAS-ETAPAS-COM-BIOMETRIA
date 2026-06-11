using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ISlotAs608OrfaoRepository
    {
        // Registra um slot pra ser apagado do AS608. Usado quando a pessoa-dona
        // é excluída — o slot ficaria fantasma no sensor sem este registro.
        Task<SlotAs608Orfao> Adicionar(int slot);
        Task<List<SlotAs608Orfao>> ListarTodos();
        Task Remover(int id);
    }
}
