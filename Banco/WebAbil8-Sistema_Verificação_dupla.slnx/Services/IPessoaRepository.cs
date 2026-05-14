using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IPessoaRepository
    {
        Pessoa Adicionar(Pessoa pessoa);
        Pessoa BuscarPorId(long id);
        Pessoa BuscarPorCPF(string cpf);
        List<Pessoa> ListarTodos();
        Pessoa Atualizar(Pessoa pessoa);
        void Remover(long id);
        void AlterarStatus(long pessoaId, bool status);
        Pessoa MarcarBiometriaCadastrada(long pessoaId);
        Pessoa SalvarTemplate(long pessoaId, byte[] template);
        Pessoa AtualizarUltimoAcesso(long pessoaId);

    }
}
