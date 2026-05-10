using Microsoft.AspNetCore.Mvc;
using InfraestruturaBloco1.Services;

namespace InfraestruturaBloco1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InfraController : ControllerBase
    {
        private readonly PasswordService _passwordService;
        private readonly EmailService _emailService;
        private readonly AesService _aesService;

        // Construtor recebe os serviços via injeção de dependência
        public InfraController(PasswordService passwordService, EmailService emailService, AesService aesService)
        {
            _passwordService = passwordService;
            _emailService = emailService;
            _aesService = aesService;
        }

        [HttpGet("hash")]
        public IActionResult Hash(string senha)
        {
            var hash = _passwordService.HashPassword(senha);
            return Ok(hash);
        }

        [HttpGet("verify")]
        public IActionResult Verify(string senha, string hash)
        {
            var isValid = _passwordService.VerifyPassword(senha, hash);
            return Ok(isValid);
        }

        [HttpPost("email")]
        public async Task<IActionResult> SendEmail(string to, string subject, string body)
        {
            await _emailService.SendEmailAsync(to, subject, body);
            return Ok("Email enviado!");
        }

        [HttpGet("encrypt")]
        public IActionResult Encrypt(string texto, string chave)
        {
            var encrypted = _aesService.Encrypt(texto, chave);
            return Ok(encrypted);
        }

        [HttpGet("decrypt")]
        public IActionResult Decrypt(string texto, string chave)
        {
            var decrypted = _aesService.Decrypt(texto, chave);
            return Ok(decrypted);
        }
    }
}
