using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

public class RelatorioAmbienteService
{
    private readonly ILogAdminRepository _logRepo;

    public RelatorioAmbienteService(ILogAdminRepository logRepo)
    {
        _logRepo = logRepo;
    }

    // Gera PDF
    public async Task Gerar(int ambienteId, DateTime de, DateTime ate, string nomeArquivo)
    {
        var logs = await _logRepo.BuscarPorAmbiente(ambienteId, de, ate);

        var cabecalhos = new List<string> { "Data", "Admin", "Ação", "Entidade", "Video" };
        var linhas = logs.Select(l => new List<string>
        {
            l.DataHora.ToString("dd/MM/yyyy HH:mm"),
            l.AdminId.ToString(),
            l.Acao,
            l.EntidadeAfetada,
            l.VideoUrl ?? "-"
        }).ToList();

        ExportService.ExportarPDF($"Relatório do Ambiente {ambienteId}", cabecalhos, linhas, nomeArquivo);
    }

    // Gera CSV
    public async Task GerarCSV(int ambienteId, DateTime de, DateTime ate, string nomeArquivo)
    {
        var logs = await _logRepo.BuscarPorAmbiente(ambienteId, de, ate);
        ExportService.ExportarCSV(logs, nomeArquivo);
    }

    // Método auxiliar para controller
    public async Task<List<LogAdmin>> BuscarLogs(int ambienteId, DateTime de, DateTime ate)
    {
        return await _logRepo.BuscarPorAmbiente(ambienteId, de, ate);
    }
}
