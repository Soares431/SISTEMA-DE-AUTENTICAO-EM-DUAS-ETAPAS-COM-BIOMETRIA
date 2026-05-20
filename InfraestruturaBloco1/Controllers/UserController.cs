using InfraestruturaBloco1.Services;
using Microsoft.AspNetCore.Mvc;

namespace InfraestruturaBloco1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AuditService _auditService;

        public UserController(AuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            _auditService.Registrar(0, "Remocao", "Usuario", id);
            return Ok($"Usuário {id} removido com sucesso!");
        }

        [HttpPost("inativar/{id}")]
        public IActionResult InativarUsuario(int id)
        {
            _auditService.Registrar(0, "Inativacao", "Usuario", id);
            return Ok($"Usuário {id} inativado!");
        }

        [HttpPost("reset-biometria/{id}")]
        public IActionResult ResetBiometria(int id)
        {
            _auditService.Registrar(0, "ResetBiometria", "Usuario", id);
            return Ok($"Usuário {id} resetada!");
        }
    }
}