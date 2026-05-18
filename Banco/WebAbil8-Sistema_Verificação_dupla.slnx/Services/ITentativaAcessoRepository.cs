using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ITentativaAcessoRepository
    {
        TentativaAcesso Adicionar(TentativaAcesso tentativa);
        TentativaAcesso BuscarPorId(int id);
        List<TentativaAcesso> ListarTodos();
        TentativaAcesso Atualizar(TentativaAcesso tentativa);
        void Remover(int id);
        TentativaAcesso Registrar(TentativaAcesso tentativa);
        List<TentativaAcesso> ListarComFiltros(long? pessoaId, int? ambienteId, bool? acessoLiberado, DateTime? dataInicio, DateTime? dataFim);
        List<TentativaAcesso> ListarPorPessoa(long pessoaId);
        List<TentativaAcesso> ListarPorAmbiente(int ambienteId);
    }
}
