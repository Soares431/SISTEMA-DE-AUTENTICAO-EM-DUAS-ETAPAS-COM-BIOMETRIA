using InfraestruturaBloco1.Data;
using InfraestruturaBloco1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InfraestruturaBloco1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // protege os endpoints
    public class CamerasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CamerasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/cameras (listagem com filtros)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Camera>>> GetCameras(
            string? nome = null,
            bool? ativa = null)
        {
            var query = _context.Cameras.AsQueryable();

            if (!string.IsNullOrEmpty(nome))
                query = query.Where(c => c.Nome.Contains(nome));

            if (ativa.HasValue)
                query = query.Where(c => c.Ativa == ativa.Value);

            return await query.ToListAsync();
        }

        // GET: api/cameras/5 (detalhe)
        [HttpGet("{id}")]
        public async Task<ActionResult<Camera>> GetCamera(int id)
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null) return NotFound();
            return camera;
        }

        // POST: api/cameras (cadastro)
        [HttpPost]
        public async Task<ActionResult<Camera>> PostCamera(Camera camera)
        {
            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCamera), new { id = camera.Id }, camera);
        }

        // PUT: api/cameras/5 (edição)
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCamera(int id, Camera camera)
        {
            if (id != camera.Id) return BadRequest();

            _context.Entry(camera).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/cameras/5 (remoção)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCamera(int id)
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null) return NotFound();

            _context.Cameras.Remove(camera);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
