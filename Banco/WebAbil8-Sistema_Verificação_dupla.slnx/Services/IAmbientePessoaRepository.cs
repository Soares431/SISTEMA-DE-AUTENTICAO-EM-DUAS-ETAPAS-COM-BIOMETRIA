using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface IAmbientePessoaRepository
    {
        AmbientePessoa AdicionarPessoa(AmbientePessoa person);
        AmbientePessoa BuscarPorId(int ambienteId, long pessoaId);

        List<AmbientePessoa> ListarTodos();
        List<Pessoa> ListarPessoasDoAmbiente(int ambienteId);
        List<Ambiente> ListarAmbientesDaPessoa(long pessoaId);
        AmbientePessoa Atualizar(AmbientePessoa person);
        void RemoverPessoa(int ambienteId, long pessoaId);

        bool PessoaTemAcesso(int ambienteId, long pessoaId);

        int ContarPessoasPorAmbiente(int ambienteId);
    }
}

