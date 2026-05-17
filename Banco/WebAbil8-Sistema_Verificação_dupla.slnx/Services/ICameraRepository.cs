using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ICameraRepository
    {
        Task<List<Camera>> ListarComFiltros(string? nome, bool? ativa);
        Task<Camera?> BuscarPorId(int id);
        Task Adicionar(Camera camera);
        Task<bool> Atualizar(Camera camera);
        Task<bool> Remover(int id);
    }
}
