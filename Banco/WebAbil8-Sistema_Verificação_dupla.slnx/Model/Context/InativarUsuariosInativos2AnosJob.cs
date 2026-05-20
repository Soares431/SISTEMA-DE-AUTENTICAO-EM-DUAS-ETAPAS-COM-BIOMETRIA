// Jobs/InativarUsuariosInativos2AnosJob.cs
using WebAbil8_Sistema_Verificação_dupla.slnx.Model.Context;

namespace WebAbil8_Sistema_Verificação_dupla.slnx.Jobs
{
    public class InativarUsuariosInativos2AnosJob
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InativarUsuariosInativos2AnosJob> _logger;

        public InativarUsuariosInativos2AnosJob(
            AppDbContext context,
            ILogger<InativarUsuariosInativos2AnosJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void Executar()
        {
            _logger.LogInformation("Executando job: InativarUsuariosInativos2Anos");

            var doisAnosAtras = DateTime.UtcNow.AddYears(-2);

            var usuarios = _context.Pessoas
                .Where(p => p.Status == "ativo"
                    && p.dataUltimoAcesso < doisAnosAtras)
                .ToList();

            foreach (var usuario in usuarios)
            {
                usuario.Status = "inativo";
                _logger.LogInformation("Usuário {id} inativado por inatividade.", usuario.Id);
            }

            _context.SaveChanges();
        }
    }
}