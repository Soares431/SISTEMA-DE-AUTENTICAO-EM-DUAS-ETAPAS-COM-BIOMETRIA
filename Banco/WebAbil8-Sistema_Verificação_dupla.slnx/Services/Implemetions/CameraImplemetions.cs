using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class CameraImplemetions : ICameraRepository
    {
        private readonly AppDbContext _context;

        public CameraImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Camera>> ListarComFiltros(string? nome, bool? ativa)
        {
            var query = _context.Cameras.AsQueryable();
            if (!string.IsNullOrEmpty(nome))
                query = query.Where(c => c.Nome.Contains(nome));
            if (ativa.HasValue)
                query = query.Where(c => c.Ativa == ativa.Value);
            return await query.ToListAsync();
        }

        public async Task<Camera?> BuscarPorId(int id) =>
            await _context.Cameras.FindAsync(id);

        public async Task Adicionar(Camera camera)
        {
            _context.Cameras.Add(camera);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> Atualizar(Camera camera)
        {
            // Usa Find pra pegar a entity já tracked (DbContext do Blazor Server é Scoped pelo
            // circuit). Marcar uma nova instância como Modified com PK conflitante causa
            // InvalidOperationException no SaveChanges porque já existe outra tracked com mesmo Id.
            var existing = await _context.Cameras.FindAsync(camera.Id);
            if (existing == null) return false;
            _context.Entry(existing).CurrentValues.SetValues(camera);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Remover(int id)
        {
            var camera = await _context.Cameras.FindAsync(id);
            if (camera == null) return false;
            _context.Cameras.Remove(camera);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Camera>> ListarPorAmbiente(int ambienteId)
        {
            return await _context.Cameras
                .Where(c => c.AmbienteId == ambienteId)
                .ToListAsync();
        }
    }

}
