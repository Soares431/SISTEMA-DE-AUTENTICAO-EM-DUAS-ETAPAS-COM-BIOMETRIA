using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GravacoesController : ControllerBase
    {
        private readonly ITentativaAcessoRepository _tentativaRepo;
        private readonly string _baseDir;

        public GravacoesController(ITentativaAcessoRepository tentativaRepo, IConfiguration config)
        {
            _tentativaRepo = tentativaRepo;
            // CAMERA_BASE_PATH é setada pelo Program.cs do Int1 apontando para a raiz do repo,
            // mesma pasta usada pelo Worker. Garantia: Path.GetFullPath normaliza.
            _baseDir = Path.GetFullPath(
                Environment.GetEnvironmentVariable("CAMERA_BASE_PATH") ?? "gravacoes");
        }

        // GET /api/gravacoes/{tentativaId}
        [HttpGet("{tentativaId:int}")]
        public IActionResult Baixar(int tentativaId)
        {
            var tentativa = _tentativaRepo.BuscarPorId(tentativaId);
            if (tentativa == null) return NotFound();
            if (string.IsNullOrWhiteSpace(tentativa.GravacaoPath)) return NotFound("Sem gravação.");

            // Anti path traversal: o path resolvido tem que estar sob _baseDir.
            // GravacaoPath agora é sempre absoluto (CameraService usa Path.GetFullPath no ctor).
            var resolved = Path.GetFullPath(tentativa.GravacaoPath);
            if (!resolved.StartsWith(_baseDir, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[Gravacoes] Path traversal bloqueado: '{resolved}' fora de '{_baseDir}'");
                return Forbid();
            }

            if (!System.IO.File.Exists(resolved))
            {
                Console.WriteLine($"[Gravacoes] Arquivo não existe em disco: {resolved}");
                return NotFound();
            }

            var stream = System.IO.File.OpenRead(resolved);
            return File(stream, "video/mp4", Path.GetFileName(resolved), enableRangeProcessing: true);
        }
    }
}
