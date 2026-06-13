using Microsoft.EntityFrameworkCore;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class SlotAs608OrfaoImplemetions : ISlotAs608OrfaoRepository
    {
        private readonly AppDbContext _context;

        public SlotAs608OrfaoImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SlotAs608Orfao> Adicionar(int slot)
        {
            var orfao = new SlotAs608Orfao { Slot = slot, CriadoEm = DateTime.UtcNow };
            _context.SlotsAs608Orfaos.Add(orfao);
            await _context.SaveChangesAsync();
            return orfao;
        }

        public async Task<List<SlotAs608Orfao>> ListarTodos()
        {
            return await _context.SlotsAs608Orfaos.ToListAsync();
        }

        public async Task Remover(int id)
        {
            var orfao = await _context.SlotsAs608Orfaos.FindAsync(id);
            if (orfao == null) return;
            _context.SlotsAs608Orfaos.Remove(orfao);
            await _context.SaveChangesAsync();
        }
    }
}

