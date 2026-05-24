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

    public void Registrar(int adminId, string acao, string entidade, int? entidadeId = null)
    {
        _logRepo.Registrar(adminId, acao, entidade, entidadeId);
    }

    public async Task RegistrarComVideo(int adminId, string acao, string entidade, int? entidadeId, string? videoUrl)
    {
        var log = new LogAdmin
        {
            AdminId = adminId,
            Acao = acao,
            EntidadeAfetada = entidade,
            EntidadeId = entidadeId,
            DataHora = DateTime.UtcNow,
            DataExpiracao = DateTime.UtcNow.AddDays(730), // 2 anos de retenção
            VideoUrl = videoUrl // agora o vídeo é registrado
        };

        await _logRepo.Registrar(log);
}


    public List<LogAdmin> Consultar(
        int? adminId = null,
        string? acao = null,
        string? entidade = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null)
    {
        return _logRepo.ListarComFiltros(adminId, acao, entidade, dataInicio, dataFim);
    }
}