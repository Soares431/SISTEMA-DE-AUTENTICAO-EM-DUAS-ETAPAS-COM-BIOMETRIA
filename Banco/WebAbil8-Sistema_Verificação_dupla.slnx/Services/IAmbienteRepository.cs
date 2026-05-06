using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IAmbienteRepository
    {
        Ambiente Adicionar(Ambiente ambiente);
        AmbientePessoa BuscarPorId(int id);
        List<AmbientePessoa> ListarTodos();
        Ambiente Atualizar(Ambiente ambiente);
        void RemoverPessoa(int id);
    }
}
