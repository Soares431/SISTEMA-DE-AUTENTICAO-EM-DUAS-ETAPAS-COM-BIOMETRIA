using WebAbil8_Sistema_Verificação_dupla.slnx.Model;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services
{
    public interface ILogAdminRepository
    {
        LogAdmin Adicionar(LogAdmin person);
        LogAdmin BuscarPorId(long id);
        List<LogAdmin> ListarTodos();
        LogAdmin Atualizar(LogAdmin person);
        void Remover(long id);

        LogAdmin Registrar(int adminId, string acao, string entidadeAfetada, int? entidadeId);
        List<LogAdmin> ListarComFiltros(int? adminId, string acao, string entidadeAfetada, DateTime? dataInicio, DateTime? dataFim);
    }
}

