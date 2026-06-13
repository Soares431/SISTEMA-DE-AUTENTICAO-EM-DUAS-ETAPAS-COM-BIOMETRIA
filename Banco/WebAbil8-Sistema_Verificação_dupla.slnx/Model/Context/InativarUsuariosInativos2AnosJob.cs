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

            var config = _context.Configuracoes.FirstOrDefault();
            int meses = Math.Clamp(config?.PeriodoInativacaoMeses ?? 24, 3, 24);
            var doisAnosAtras = DateTime.UtcNow.AddMonths(-meses);
            _logger.LogInformation("Período de inativação configurado: {meses} meses.", meses);

            var usuarios = _context.Pessoas
                .Where(p => p.Status == "ativo"
                    && (p.dataUltimoAcesso < doisAnosAtras
                        || (p.dataUltimoAcesso == null && p.dataCadastro < doisAnosAtras)))
                .ToList();

            foreach (var usuario in usuarios)
            {
                usuario.Status = "inativo";

                var vinculos = _context.AmbientesPessoas
                    .Where(ap => ap.PessoaId == usuario.Id)
                    .ToList();
                _context.AmbientesPessoas.RemoveRange(vinculos);
                _logger.LogInformation("Usuário {id} inativado por inatividade ({count} ambientes removidos).", usuario.Id, vinculos.Count);
            }

            _context.SaveChanges();
        }
    }
}
