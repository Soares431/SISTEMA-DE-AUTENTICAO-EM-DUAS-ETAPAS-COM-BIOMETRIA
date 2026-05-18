using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;
using Microsoft.EntityFrameworkCore;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class ConfiguracaoImplemetions : IConfiguracaoRepository
    {

        private readonly AppDbContext _context;

        public ConfiguracaoImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Configuracao> BuscarPorChave()
        {
            // Sempre retorna o registro único de configuração (Id = 1)
            return await _context.Configuracoes.FirstOrDefaultAsync();
        }

        public async Task<Configuracao> Atualizar(Configuracao configuracao)
        {
            var existing = await _context.Configuracoes.FirstOrDefaultAsync();
            if (existing == null) throw new ArgumentNullException("Configuração não encontrada.");
            _context.Entry(existing).CurrentValues.SetValues(configuracao);
            await _context.SaveChangesAsync();
            return existing;
        }
    }
}
