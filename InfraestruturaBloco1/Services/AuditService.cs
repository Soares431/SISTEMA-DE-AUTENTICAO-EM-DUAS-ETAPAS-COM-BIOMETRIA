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

    public void RegistrarComVideo(int adminId, string acao, string entidade, int? entidadeId, string? videoPath)
    {
        // VideoUrl não existe no LogAdmin — registra a ação normalmente
        // o path do vídeo fica em TentativaAcesso.GravacaoPath
        _logRepo.Registrar(adminId, acao, entidade, entidadeId);
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