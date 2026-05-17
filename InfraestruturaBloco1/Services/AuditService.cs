using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace InfraestruturaBloco1.Services;

public class AuditService
{
    private readonly ILogAdminRepository _logRepo;

    public AuditService(ILogAdminRepository logRepo)
    {
        _logRepo = logRepo;
    }

    public async Task RegistrarAsync(int adminId, string acao, string entidade, int? entidadeId = null)
    {
        var log = new LogAdmin
        {
            AdminId = adminId,
            Acao = acao,
            EntidadeAfetada = entidade,
            EntidadeId = entidadeId,
            DataHora = DateTime.UtcNow,
            DataExpiracao = DateTime.UtcNow.AddDays(180)
        };

        await _logRepo.Registrar(log);
    }

    public async Task<List<LogAdmin>> ConsultarAsync(
        int? adminId = null,
        string? acao = null,
        string? entidade = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null)
    {
        return await _logRepo.ListarComFiltros(adminId, acao, entidade, dataInicio, dataFim);
    }
}