using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ISenhaRepository
    {
        SenhaDisponivel Adicionar(SenhaDisponivel senha);
        SenhaDisponivel BuscarDisponivel(string id);
        List<SenhaDisponivel> ListarTodos();
        SenhaDisponivel Atualizar(SenhaDisponivel senha);
        void Remover(string id);

        SenhaDisponivel BuscarDisponivel();

        SenhaDisponivel MarcarEmUso(string senha, bool emUso);

        SenhaDisponivel Liberar(string senha);
    }
}
