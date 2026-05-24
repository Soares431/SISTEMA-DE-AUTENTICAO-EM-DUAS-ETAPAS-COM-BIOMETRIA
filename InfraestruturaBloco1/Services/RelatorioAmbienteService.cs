using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace InfraestruturaBloco1.Services;

public class RelatorioAmbienteService
{
    private readonly ILogAdminRepository _logRepo;

    public RelatorioAmbienteService(ILogAdminRepository logRepo)
    {
        _logRepo = logRepo;
    }

    public void Gerar(int ambienteId, DateTime de, DateTime ate, string nomeArquivo)
    {
        var logs = _logRepo.ListarComFiltros(null, null, null, de, ate)
            .Where(l => l.EntidadeId == ambienteId)
            .ToList();

        var cabecalhos = new List<string> { "Data", "Admin", "Ação", "Entidade" };
        var linhas = logs.Select(l => new List<string>
        {
            l.DataHora.ToString("dd/MM/yyyy HH:mm"),
            l.AdminId.ToString(),
            l.Acao,
            l.EntidadeAfetada
        }).ToList();

        ExportService.ExportarPDF($"Relatório do Ambiente {ambienteId}", cabecalhos, linhas, nomeArquivo);
    }

    public void GerarCSV(int ambienteId, DateTime de, DateTime ate, string nomeArquivo)
    {
        var logs = _logRepo.ListarComFiltros(null, null, null, de, ate)
            .Where(l => l.EntidadeId == ambienteId)
            .ToList();

        ExportService.ExportarCSV(logs, nomeArquivo);
    }

    public List<LogAdmin> BuscarLogs(int ambienteId, DateTime de, DateTime ate)
    {
        return _logRepo.ListarComFiltros(null, null, null, de, ate)
            .Where(l => l.EntidadeId == ambienteId)
            .ToList();
    }
}