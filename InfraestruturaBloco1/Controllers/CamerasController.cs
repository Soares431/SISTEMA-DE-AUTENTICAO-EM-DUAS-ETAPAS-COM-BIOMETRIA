using Microsoft.AspNetCore.Mvc;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;
using InfraestruturaBloco1.Services;

namespace InfraestruturaBloco1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // protege todos os endpoints
    public class CamerasController : ControllerBase
    {
        private readonly ICameraRepository _cameraRepo;
        private readonly CameraService _cameraService;

        public CamerasController(ICameraRepository cameraRepo, CameraService cameraService)
        {
            _cameraRepo = cameraRepo;
            _cameraService = cameraService;
        }

        // GET: api/cameras
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Camera>>> GetCameras(string? nome = null, bool? ativa = null)
        {
            var cameras = await _cameraRepo.ListarComFiltros(nome, ativa);
            return Ok(cameras);
        }

        // GET: api/cameras/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Camera>> GetCamera(int id)
        {
            var camera = await _cameraRepo.BuscarPorId(id);
            if (camera == null) return NotFound();
            return Ok(camera);
        }

        // GET: api/cameras/stream/5
        [HttpGet("stream/{id}")]
        public async Task<ActionResult<string>> GetCameraStream(int id)
        {
            var url = await _cameraService.ObterUrlStream(id);
            if (url == null) return NotFound("Câmera não encontrada ou sem URL RTSP configurada.");
            return Ok(url);
        }

        // POST: api/cameras
        [HttpPost]
        public async Task<ActionResult<Camera>> PostCamera(Camera camera)
        {
            await _cameraRepo.Adicionar(camera);
            return CreatedAtAction(nameof(GetCamera), new { id = camera.Id }, camera);
        }

        // PUT: api/cameras/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCamera(int id, Camera camera)
        {
            if (id != camera.Id) return BadRequest();

            var atualizado = await _cameraRepo.Atualizar(camera);
            if (!atualizado) return NotFound();

            return NoContent();
        }

        // DELETE: api/cameras/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCamera(int id)
        {
            var removido = await _cameraRepo.Remover(id);
            if (!removido) return NotFound();

            return NoContent();
        }
    }
}
