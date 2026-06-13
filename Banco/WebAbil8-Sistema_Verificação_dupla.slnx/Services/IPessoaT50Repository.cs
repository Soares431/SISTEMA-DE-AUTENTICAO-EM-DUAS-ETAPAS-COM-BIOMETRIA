using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IPessoaT50Repository
    {

        PessoaT50 Adicionar(long pessoaId, int t50Id);

        void Remover(long pessoaId, int t50Id);

        List<DispositivoT50> ListarT50sDaPessoa(long pessoaId);

        List<Pessoa> ListarPessoasDoT50(int t50Id);

        bool EstaCadastrada(long pessoaId, int t50Id);

        int ContarPessoasNoT50(int t50Id);
    }
}

