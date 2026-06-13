using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ISlotAs608OrfaoRepository
    {

        Task<SlotAs608Orfao> Adicionar(int slot);
        Task<List<SlotAs608Orfao>> ListarTodos();
        Task Remover(int id);
    }
}

