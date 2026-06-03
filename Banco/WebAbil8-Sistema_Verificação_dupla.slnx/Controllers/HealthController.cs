using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Controllers
{
    // Endpoint público de health check — útil pra monitoramento e oncall.
    // Não exige autenticação pra permitir verificação externa simples.
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext _db;

        public HealthController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var checks = new Dictionary<string, object>();
            bool ok = true;

            // Banco
            try
            {
                var conn = _db.Database.GetDbConnection();
                if (conn.State == System.Data.ConnectionState.Closed) conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM pessoa";
                cmd.ExecuteScalar();
                checks["banco"] = new { status = "ok" };
            }
            catch (Exception ex)
            {
                ok = false;
                checks["banco"] = new { status = "fail", erro = ex.Message };
            }

            // FFmpeg — validação inline (Int1 não referencia InfraestruturaBloco1 por restrição de dependência circular)
            var ffmpegPath = Environment.GetEnvironmentVariable("FFMPEG_PATH") ?? "ffmpeg";
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                bool ffmpegOk = proc != null && proc.WaitForExit(5000) && proc.ExitCode == 0;
                checks["ffmpeg"] = new
                {
                    status = ffmpegOk ? "ok" : "indisponivel",
                    path = ffmpegPath
                };
                if (!ffmpegOk) ok = false;
            }
            catch (Exception ex)
            {
                ok = false;
                checks["ffmpeg"] = new { status = "fail", erro = ex.Message, path = ffmpegPath };
            }

            // SMTP — só reporta config, não tenta conectar
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            checks["smtp"] = new
            {
                status = string.IsNullOrEmpty(smtpHost) ? "nao_configurado" : "configurado",
                host = smtpHost ?? "(vazio — fallback console)"
            };

            // Pools de senha e código
            try
            {
                var senhasLivres = _db.SenhasDisponiveis.Count(s => !s.EmUso);
                var codigosLivres = _db.CodigosDisponiveis.Count(c => !c.EmUso);
                checks["pools"] = new { senhas_livres = senhasLivres, codigos_livres = codigosLivres };
                if (senhasLivres < 100 || codigosLivres < 100) ok = false;
            }
            catch (Exception ex)
            {
                ok = false;
                checks["pools"] = new { status = "fail", erro = ex.Message };
            }

            var resposta = new
            {
                status = ok ? "ok" : "degraded",
                timestamp = DateTime.UtcNow,
                checks
            };

            return ok ? Ok(resposta) : StatusCode(503, resposta);
        }
    }
}
