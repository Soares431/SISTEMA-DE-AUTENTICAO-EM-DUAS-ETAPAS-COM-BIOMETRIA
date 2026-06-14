using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Services.Implemetions
{
    public class StatusServiceImplemetions : IStatusService
    {
        private readonly AppDbContext _context;

        public StatusServiceImplemetions(AppDbContext context)
        {
            _context = context;
        }

        public async Task AtualizarStatus(long pessoaId)
        {
            var pessoa = await _context.Pessoas.FindAsync(pessoaId);
            if (pessoa == null) throw new ArgumentNullException("Pessoa não encontrada");

            var temAmbiente = _context.AmbientesPessoas
                .Any(ap => ap.PessoaId == pessoaId);

            pessoa.Status = temAmbiente ? "ativo" : "inativo"; ;

            _context.Pessoas.Update(pessoa);
            await _context.SaveChangesAsync();
        }
    }
}

