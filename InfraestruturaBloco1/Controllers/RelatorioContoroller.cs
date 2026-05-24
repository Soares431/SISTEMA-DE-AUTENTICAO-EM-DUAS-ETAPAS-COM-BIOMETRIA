using Microsoft.AspNetCore.Mvc;
using InfraestruturaBloco1.Services;

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

        [HttpGet("ambiente/{id}/pdf")]
        public IActionResult GerarRelatorioPDF(int id, DateTime de, DateTime ate)
        {
            var nomeArquivo = $"Relatorio_Ambiente_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            _relatorioAmbienteService.Gerar(id, de, ate, nomeArquivo);
            var bytes = System.IO.File.ReadAllBytes(nomeArquivo);
            return File(bytes, "application/pdf", nomeArquivo);
        }

        [HttpGet("ambiente/{id}/csv")]
        public IActionResult GerarRelatorioCSV(int id, DateTime de, DateTime ate)
        {
            var nomeArquivo = $"Relatorio_Ambiente_{id}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            _relatorioAmbienteService.GerarCSV(id, de, ate, nomeArquivo);
            var bytes = System.IO.File.ReadAllBytes(nomeArquivo);
            return File(bytes, "text/csv", nomeArquivo);
        }

        [HttpGet("ambiente/{id}/json")]
        public IActionResult GerarRelatorioJSON(int id, DateTime de, DateTime ate)
        {
            var logs = _relatorioAmbienteService.BuscarLogs(id, de, ate);
            return Ok(logs);
        }
    }
}