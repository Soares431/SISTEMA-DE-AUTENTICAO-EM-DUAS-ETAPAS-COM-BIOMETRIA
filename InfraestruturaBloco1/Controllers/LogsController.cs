using Microsoft.AspNetCore.Authorization;
using InfraestruturaBloco1.Services;

using Microsoft.AspNetCore.Mvc;

namespace InfraestruturaBloco1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly AuditService _auditService;

        public LogsController(AuditService auditService)
        {
            _auditService = auditService;
        }

        // GET /api/logs?admin=Joao&acao=Remocao&dataInicio=2026-05-01&dataFim=2026-05-15
        [HttpGet]
        public async Task<IActionResult> GetLogs(
            [FromQuery] string? admin,
            [FromQuery] string? acao,
            [FromQuery] string? entidade,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim)
        {
            var logs = await _auditService.ConsultarAsync(admin, acao, entidade, dataInicio, dataFim);
            return Ok(logs);
        }
    }
}
