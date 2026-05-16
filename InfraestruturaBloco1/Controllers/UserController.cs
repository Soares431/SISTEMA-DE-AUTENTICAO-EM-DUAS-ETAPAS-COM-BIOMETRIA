using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Lógica de remoção do usuário no banco
            // Exemplo: _context.Users.Remove(user);

            var admin = User.Identity?.Name ?? "Sistema";
            await _auditService.RegistrarAsync(admin, "Remocao", $"Usuario:{id}");

            return Ok($"Usuário {id} removido com sucesso!");
        }

        [HttpPost("inativar/{id}")]
        public async Task<IActionResult> InativarUsuario(int id)
        {
            // Lógica para inativar usuário
            // Exemplo: user.Ativo = false;

            var admin = User.Identity?.Name ?? "Sistema";
            await _auditService.RegistrarAsync(admin, "Inativacao", $"Usuario:{id}");

            return Ok($"Usuário {id} inativado!");
        }

        [HttpPost("reset-biometria/{id}")]
        public async Task<IActionResult> ResetBiometria(int id)
        {
            // Lógica para resetar biometria
            // Exemplo: user.Biometria = null;

            var admin = User.Identity?.Name ?? "Sistema";
            await _auditService.RegistrarAsync(admin, "ResetBiometria", $"Usuario:{id}");

            return Ok($"Biometria do usuário {id} resetada!");
        }
    }
}
