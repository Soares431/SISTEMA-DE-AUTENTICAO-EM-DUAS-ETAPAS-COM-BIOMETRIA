using InfraestruturaBloco1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace InfraestruturaBloco1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // protege todos os endpoints
    public class UserController : ControllerBase
    {
        private readonly AuditService _auditService;

        public UserController(AuditService auditService)
        {
            _auditService = auditService;
        }

        private int GetAdminIdFromToken()
        {
            // tenta pegar o claim "NameIdentifier" (padrão do JWT)
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                        ?? User.FindFirst("id")?.Value 
                        ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(claim))
                throw new UnauthorizedAccessException("Token inválido: não contém adminId.");

            return int.Parse(claim);
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var adminId = GetAdminIdFromToken();
            _auditService.Registrar(adminId, "Remocao", "Usuario", id);
            return Ok($"Usuário {id} removido com sucesso!");
        }

        [HttpPost("inativar/{id}")]
        public IActionResult InativarUsuario(int id)
        {
            var adminId = GetAdminIdFromToken();
            _auditService.Registrar(adminId, "Inativacao", "Usuario", id);
            return Ok($"Usuário {id} inativado!");
        }

        [HttpPost("reset-biometria/{id}")]
        public IActionResult ResetBiometria(int id)
        {
            var adminId = GetAdminIdFromToken();
            _auditService.Registrar(adminId, "ResetBiometria", "Usuario", id);
            return Ok($"Usuário {id} resetada!");
        }
    }
}
