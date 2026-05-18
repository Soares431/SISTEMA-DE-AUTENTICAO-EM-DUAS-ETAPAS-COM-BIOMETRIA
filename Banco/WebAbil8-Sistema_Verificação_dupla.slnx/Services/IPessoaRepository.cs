using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IPessoaRepository
    {
        Task<Pessoa> Adicionar(Pessoa pessoa);
        Task<Pessoa> BuscarPorId(long id);
        Task<Pessoa> BuscarPorCPF(string cpf);
        Task<List<Pessoa>> ListarTodos();
        Task<Pessoa> Atualizar(Pessoa pessoa);
        Task Remover(long id);
        Task AlterarStatus(long pessoaId, bool status);
        Task<Pessoa> MarcarBiometriaCadastrada(long pessoaId);
        Task<Pessoa> SalvarTemplate(long pessoaId, byte[] template);
        Task<Pessoa> AtualizarUltimoAcesso(long pessoaId);

    }
}
