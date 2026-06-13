using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IPessoaRepository
    {
        Task<Pessoa> Adicionar(Pessoa pessoa);
        Task<Pessoa> BuscarPorId(long id);
        Task<Pessoa?> BuscarPorCodigoUsuario(string codigoUsuario);
        Task<Pessoa> BuscarPorCPF(string cpf);
        Task<List<Pessoa>> ListarTodos();
        Task<Pessoa> Atualizar(Pessoa pessoa);
        Task Remover(long id);
        Task AlterarStatus(long pessoaId, bool status);
        Task<Pessoa> MarcarBiometriaCadastrada(long pessoaId);
        Task<Pessoa> SalvarTemplate(long pessoaId, byte[] template);
        Task<Pessoa> AtualizarUltimoAcesso(long pessoaId);
        Task<Pessoa> AtualizarSenha(long pessoaId, string novaSenhaClear, string novoSenhaHash);

        Task<int?> AlocarSlotAs608Livre();

        Task DefinirSlotAs608(long pessoaId, int slot);

        Task EnfileirarSlotParaApagar(long pessoaId);

        Task<List<Pessoa>> ListarSlotsPendentesApagar();

        Task LimparSlotPendenteApagar(long pessoaId);
    }
}

