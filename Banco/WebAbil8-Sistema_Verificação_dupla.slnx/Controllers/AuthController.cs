using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var admin = _context.Administradores
                .FirstOrDefault(a => a.Login == request.Login);

            if (admin == null || !BCrypt.Net.BCrypt.Verify(request.Senha, admin.SenhaHash))
                return Unauthorized("Login ou senha inválidos.");

            var token = GerarToken(admin.Id, admin.NomeCompleto);

            return Ok(new
            {
                token,
                adminId = admin.Id,
                nomeCompleto = admin.NomeCompleto
            });
        }

        private string GerarToken(int adminId, string nomeCompleto)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("adminId", adminId.ToString()),
                new Claim("nomeCompleto", nomeCompleto),
                new Claim(JwtRegisteredClaimNames.Sub, adminId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Login { get; set; } = "";
        public string Senha { get; set; } = "";
    }
}