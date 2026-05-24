using Microsoft.AspNetCore.Mvc;
using InfraestruturaBloco1.Services;

namespace InfraestruturaBloco1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // protege todos os endpoints
    public class LogsController : ControllerBase
    {
        private readonly AuditService _auditService;

        public LogsController(AuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult GetLogs(
            [FromQuery] int? adminId,
            [FromQuery] string? acao,
            [FromQuery] string? entidade,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim)
        {
            var logs = _auditService.Consultar(adminId, acao, entidade, dataInicio, dataFim);
            return Ok(logs);
        }

        [HttpGet("{id}")]
        public IActionResult GetLog(int id)
        {
            var logs = _auditService.Consultar();
            var log = logs.FirstOrDefault(l => l.Id == id);
            if (log == null) return NotFound();
            return Ok(log);
        }
    }
}