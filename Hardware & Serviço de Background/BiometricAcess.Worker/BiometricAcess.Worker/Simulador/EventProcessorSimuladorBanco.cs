using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.Simulador
{
    // Simulador que grava eventos reais no banco SQLite do Int1
    // Requer pelo menos um Ambiente e um DispositivoT50 cadastrados no painel
    public class EventProcessorSimuladorBanco : IEventProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public EventProcessorSimuladorBanco(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task Processar(EventoAcesso evento)
        {
            using var scope = _scopeFactory.CreateScope();
            var pessoaRepo          = scope.ServiceProvider.GetRequiredService<IPessoaRepository>();
            var tentativaRepo       = scope.ServiceProvider.GetRequiredService<ITentativaAcessoRepository>();
            var ambienteRepo        = scope.ServiceProvider.GetRequiredService<IAmbienteRepository>();
            var dispositivoRepo     = scope.ServiceProvider.GetRequiredService<IDispositivoT50Repository>();
            var configRepo          = scope.ServiceProvider.GetRequiredService<IConfiguracaoRepository>();
            var ambientePessoaRepo  = scope.ServiceProvider.GetRequiredService<IAmbientePessoaRepository>();

            // Resolve o ambiente pelo IP do dispositivo; cai no primeiro disponível como fallback
            var dispositivos = dispositivoRepo.ListarTodos();
            var dispositivo  = dispositivos.FirstOrDefault(d => d.EnderecoIP == evento.IpDispositivo);
            var ambientes    = ambienteRepo.ListarTodos();
            var ambiente     = dispositivo != null
                ? ambientes.FirstOrDefault(a => a.DispositivoT50Id == dispositivo.Id)
                : ambientes.FirstOrDefault();

            if (ambiente == null)
            {
                Console.WriteLine("[SimuladorBanco] Nenhum ambiente encontrado. Cadastre um ambiente no painel antes de usar o simulador com banco.");
                return;
            }

            var config       = await configRepo.BuscarPorChave();
            var retencaoDias = config?.RetencaoGravacoesTentativasDias ?? 90;

            var pessoa = await pessoaRepo.BuscarPorId(evento.PessoaID);

            bool acessoLiberado;
            string? motivoNegacao = null;

            if (pessoa == null)
            {
                acessoLiberado = false;
                motivoNegacao  = "nao_cadastrado";
            }
            else if (pessoa.Status != "ativo")
            {
                acessoLiberado = false;
                motivoNegacao  = "inativo";
            }
            else if (!ambientePessoaRepo.PessoaTemAcesso(ambiente.Id, pessoa.Id))
            {
                acessoLiberado = false;
                motivoNegacao  = "sem_permissao";
            }
            else
            {
                acessoLiberado = true;
                await pessoaRepo.AtualizarUltimoAcesso(pessoa.Id);
            }

            var tentativa = new TentativaAcesso
            {
                PessoaId        = pessoa?.Id,
                AmbienteId      = ambiente.Id,
                DataHora        = evento.DataHora,
                AcessoLiberado  = acessoLiberado,
                MotivoNegacao   = motivoNegacao,
                TipoVerificacao = evento.TipoVerificacao,
                DataExpiracao   = DateTime.UtcNow.AddDays(retencaoDias)
            };

            tentativaRepo.Adicionar(tentativa);

            Console.WriteLine($"[SimuladorBanco] Pessoa {evento.PessoaID} | {(acessoLiberado ? "LIBERADO" : $"NEGADO — {motivoNegacao}")} | Ambiente: {ambiente.Nome}");
        }
    }
}
