using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ISenhaRepository
    {
        SenhaDisponivel Adicionar(SenhaDisponivel senha);
        SenhaDisponivel BuscarDisponivel(string id); // busca por senha específica
        SenhaDisponivel BuscarDisponivel();
        List<SenhaDisponivel> ListarTodos();
        SenhaDisponivel Atualizar(SenhaDisponivel senha);
        void Remover(string id);


        SenhaDisponivel MarcarEmUso(string senha, bool emUso);

        SenhaDisponivel Liberar(string senha);
    }
}
