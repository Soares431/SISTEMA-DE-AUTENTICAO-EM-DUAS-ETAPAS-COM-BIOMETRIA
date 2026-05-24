using Microsoft.AspNetCore.Mvc;

namespace InfraestruturaBloco1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RelatorioController : ControllerBase
    {
        private readonly RelatorioAmbienteService _relatorioAmbienteService;

        public RelatorioController(RelatorioAmbienteService relatorioAmbienteService)
        {
            _relatorioAmbienteService = relatorioAmbienteService;
        }

        // GET: api/relatorio/ambiente/{id}/pdf
        [HttpGet("ambiente/{id}/pdf")]
        public async Task<IActionResult> GerarRelatorioPDF(int id, DateTime de, DateTime ate)
        {
            var nomeArquivo = $"Relatorio_Ambiente_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

            await _relatorioAmbienteService.Gerar(id, de, ate, nomeArquivo);

            var bytes = await System.IO.File.ReadAllBytesAsync(nomeArquivo);
            return File(bytes, "application/pdf", nomeArquivo);
        }

        // GET: api/relatorio/ambiente/{id}/csv
        [HttpGet("ambiente/{id}/csv")]
        public async Task<IActionResult> GerarRelatorioCSV(int id, DateTime de, DateTime ate)
        {
            var nomeArquivo = $"Relatorio_Ambiente_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

            await _relatorioAmbienteService.GerarCSV(id, de, ate, nomeArquivo);

            var bytes = await System.IO.File.ReadAllBytesAsync(nomeArquivo);
            return File(bytes, "text/csv", nomeArquivo);
        }

        // GET: api/relatorio/ambiente/{id}/json
        [HttpGet("ambiente/{id}/json")]
        public async Task<IActionResult> GerarRelatorioJSON(int id, DateTime de, DateTime ate)
        {
            var logs = await _relatorioAmbienteService.BuscarLogs(id, de, ate);
            return Ok(logs);
        }
    }
}
