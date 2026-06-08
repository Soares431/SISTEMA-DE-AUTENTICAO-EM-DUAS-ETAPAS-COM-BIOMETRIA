using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.Security.Claims;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
namespace WebAbil8_Sistema_Verificação_dupla.slnx.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : ControllerBase
    {
        private IPessoaRepository _pessoaRepository;
        private readonly ILogAdminRepository _logRepository;
        private readonly IAmbientePessoaRepository _ambientePessoaRepository;
        private readonly IDispositivoT50Repository _dispositivoRepository;
        private readonly ICodigoRepository _codigoRepository;
        private readonly ISenhaRepository _senhaRepository;
        private readonly IConfiguration _config;
        private readonly ILogger<PersonController> _logger;

        public PersonController(IPessoaRepository pessoaRepository,
            ILogAdminRepository logRepository,
            IAmbientePessoaRepository ambientePessoaRepository,
            IDispositivoT50Repository dispositivoRepository,
            ICodigoRepository codigoRepository,
            ISenhaRepository senhaRepository,
            IConfiguration config,
            ILogger<PersonController> logger)
        {
            _pessoaRepository = pessoaRepository;
            _logRepository = logRepository;
            _ambientePessoaRepository = ambientePessoaRepository;
            _dispositivoRepository = dispositivoRepository;
            _codigoRepository = codigoRepository;
            _senhaRepository = senhaRepository;
            _config = config;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("Fetching all persons");
            return Ok(await _pessoaRepository.ListarTodos());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            _logger.LogInformation("Fetching person with ID {id}", id);
            var person = await _pessoaRepository.BuscarPorId(id);
            if (person == null)
            {
                _logger.LogWarning("Person with ID {id} not found!", id);
                return NotFound();
            }
            return Ok(person);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Pessoa person)
        {
            _logger.LogInformation("Create new Person: {name}", person.Nome);
            try
            {
                var createdPerson = await _pessoaRepository.Adicionar(person);
                if (createdPerson == null)
                {
                    _logger.LogError("Failed to create person");
                    return NotFound();
                }
                _logger.LogDebug("Person created successfully: {name}", createdPerson.Nome);
                return Ok(createdPerson);
            }
            catch (InvalidOperationException ex)
            {
                // Validações da camada de serviço (CPF duplicado, Senha == ID, etc).
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody] Pessoa person)
        {
            _logger.LogInformation("Update Person with ID {id}", person.Id);
            try
            {
                var updatedPerson = await _pessoaRepository.Atualizar(person);
                if (updatedPerson == null)
                {
                    _logger.LogError("Failed to update person with ID {id}", person.Id);
                    return NotFound();
                }
                _logger.LogDebug("Person updated successfully: {name}", updatedPerson.Nome);
                return Ok(updatedPerson);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        // DELETE /api/person/{id}
        // Remoção definitiva — libera CodigoUsuario e Senha de volta aos pools, remove vínculos
        // de ambientes e auditoria. Use com cuidado: ação irreversível.
        // Para revogar acesso temporariamente, use o endpoint de inativação.
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            _logger.LogInformation("Delete Person with ID {id}", id);
            var pessoa = await _pessoaRepository.BuscarPorId(id);
            if (pessoa == null) return NotFound();

            // 1. Snapshot dos ambientes ANTES de remover (necessário pra decrementar T50)
            var ambientes = _ambientePessoaRepository.ListarAmbientesDaPessoa(id);
            var tinhaBiometria = pessoa.biometriaCadastrada != null;

            // 2. Remove vínculos com ambientes
            foreach (var amb in ambientes)
                _ambientePessoaRepository.RemoverPessoa(amb.Id, id);

            // 3. Decrementa DigitaisCadastradas do T50 de cada ambiente que a pessoa frequentava
            if (tinhaBiometria)
            {
                foreach (var amb in ambientes)
                {
                    var dispositivo = _dispositivoRepository.BuscarPorId(amb.DispositivoT50Id);
                    if (dispositivo != null && dispositivo.DigitaisCadastradas > 0)
                    {
                        dispositivo.DigitaisCadastradas--;
                        _dispositivoRepository.Atualizar(dispositivo);
                    }
                }
            }

            // 4. Libera CodigoUsuario de volta ao pool
            if (!string.IsNullOrEmpty(pessoa.CodigoUsuario))
            {
                try { _codigoRepository.Liberar(pessoa.CodigoUsuario); }
                catch (Exception ex) { _logger.LogWarning(ex, "Falha ao liberar código {c}", pessoa.CodigoUsuario); }
            }

            // 5. Libera a senha de volta ao pool (busca por PessoaId)
            try
            {
                var senhas = _senhaRepository.ListarTodos();
                foreach (var s in senhas.Where(s => s.PessoaId == id))
                {
                    s.EmUso = false;
                    s.PessoaId = null;
                    _senhaRepository.Atualizar(s);
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Falha ao liberar senha da pessoa {id}", id); }

            // 6. Remove a pessoa
            await _pessoaRepository.Remover(id);

            // 7. Registra auditoria
            var adminIdClaim = User.FindFirst("adminId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(adminIdClaim, out var adminId))
                _logRepository.Registrar(adminId, "Remocao", "Pessoa", (int)id);

            _logger.LogInformation("Person with ID {id} permanently deleted", id);
            return NoContent();
        }

        // INT-02 — Reenvio de credenciais (ID + senha) por email
        // Aceita os dois paths: /reenviar-senha (legado) e /reenviar-credenciais (novo)
        [HttpPost("{id}/reenviar-senha")]
        [HttpPost("{id}/reenviar-credenciais")]
        public async Task<IActionResult> ReenviarCredenciais(long id)
        {
            var pessoa = await _pessoaRepository.BuscarPorId(id);
            if (pessoa == null) return NotFound();
            if (string.IsNullOrWhiteSpace(pessoa.Email)) return BadRequest("Pessoa sem email cadastrado.");
            if (string.IsNullOrWhiteSpace(pessoa.senhaClear)) return BadRequest("Senha não disponível para reenvio.");

            string senhaPlain;
            try
            {
                senhaPlain = AesHelper.Decrypt(pessoa.senhaClear, AesHelper.ResolverChave(_config));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao descriptografar senha da pessoa {id}", id);
                return StatusCode(500, "Falha ao recuperar a senha cifrada.");
            }

            var idMostrar = pessoa.CodigoUsuario ?? pessoa.Id.ToString();
            var corpoHtml = $@"<h2>Olá, {pessoa.Nome}!</h2>
<p>Seguem suas credenciais de acesso ao sistema do 5º CTA:</p>
<table style=""border-collapse:collapse"">
<tr><td style=""padding:6px 12px""><strong>ID do usuário (T50):</strong></td><td style=""padding:6px 12px;font-family:monospace;font-size:1.1em"">{idMostrar}</td></tr>
<tr><td style=""padding:6px 12px""><strong>Senha provisória:</strong></td><td style=""padding:6px 12px;font-family:monospace;font-size:1.1em"">{senhaPlain}</td></tr>
</table>
<p>No terminal T50, digite o <strong>ID</strong> seguido da <strong>senha</strong> para liberar o acesso.</p>
<p>Não compartilhe com terceiros.</p>";

            var fallback = false;
            try
            {
                var host = Environment.GetEnvironmentVariable("SMTP_HOST");
                var portStr = Environment.GetEnvironmentVariable("SMTP_PORT");
                var user = Environment.GetEnvironmentVariable("SMTP_USER");
                var pass = Environment.GetEnvironmentVariable("SMTP_PASS");

                if (string.IsNullOrEmpty(host) || !int.TryParse(portStr, out var port))
                {
                    fallback = true;
                    Console.WriteLine($"[FALLBACK] SMTP não configurado. Credenciais de {pessoa.Email}: ID={idMostrar} Senha={senhaPlain}");
                }
                else
                {
                    var msg = new MimeMessage();
                    msg.From.Add(new MailboxAddress("Sistema 5º CTA", user ?? "noreply@5cta.local"));
                    msg.To.Add(new MailboxAddress(pessoa.Nome, pessoa.Email));
                    msg.Subject = "Credenciais de acesso — 5º CTA";
                    msg.Body = new TextPart("html") { Text = corpoHtml };

                    using var smtp = new SmtpClient();
                    await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                    if (!string.IsNullOrEmpty(user)) await smtp.AuthenticateAsync(user, pass);
                    await smtp.SendAsync(msg);
                    await smtp.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                fallback = true;
                _logger.LogWarning(ex, "Falha SMTP — usando fallback console para pessoa {id}", id);
                Console.WriteLine($"[FALLBACK] Erro SMTP. Credenciais de {pessoa.Email}: ID={idMostrar} Senha={senhaPlain}");
            }

            // Registra auditoria
            var adminIdClaim = User.FindFirst("adminId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(adminIdClaim, out var adminId))
                _logRepository.Registrar(adminId, "ReenvioCredenciais", "Pessoa", (int)id);

            return Ok(new { sucesso = true, fallback, mensagem = fallback ? "SMTP indisponível — credenciais exibidas no console do servidor." : "Email enviado." });
        }
    }

}
