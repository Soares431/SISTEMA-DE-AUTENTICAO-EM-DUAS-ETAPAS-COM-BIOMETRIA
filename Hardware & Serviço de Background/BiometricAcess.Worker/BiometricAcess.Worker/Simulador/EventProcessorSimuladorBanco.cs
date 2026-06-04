using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;
using InfraestruturaBloco1.Services;
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
            var ambienteT50Repo     = scope.ServiceProvider.GetRequiredService<IAmbienteT50Repository>();

            // Resolve o ambiente pelo IP do dispositivo. Ambiente sem T50 vinculado NÃO recebe evento —
            // o painel agora bloqueia criação sem T50, mas a defesa em profundidade evita
            // que um T50 deletado deixe o ambiente em estado inconsistente.
            var dispositivos = dispositivoRepo.ListarTodos();
            var dispositivo  = dispositivos.FirstOrDefault(d => d.EnderecoIP == evento.IpDispositivo);

            if (dispositivo == null)
            {
                Console.WriteLine($"[SimuladorBanco] Nenhum T50 cadastrado com IP {evento.IpDispositivo} — evento ignorado.");
                return;
            }

            // Multi-T50: um T50 pode estar vinculado a múltiplos ambientes. Pega o primeiro
            // que estiver vinculado (ehPrincipal primeiro pela ordenação do repo).
            var ambientesDoT50 = ambienteT50Repo.ListarAmbientesDoT50(dispositivo.Id);
            var ambiente       = ambientesDoT50.FirstOrDefault();

            if (ambiente == null)
            {
                Console.WriteLine($"[SimuladorBanco] T50 '{dispositivo.Nome}' ({dispositivo.EnderecoIP}) não está vinculado a nenhum ambiente — evento ignorado.");
                return;
            }

            // Heartbeat: dispositivo está vivo pois acabou de mandar evento
            if (dispositivo != null)
                dispositivoRepo.RegistrarHeartbeat(dispositivo.EnderecoIP);

            var config       = await configRepo.BuscarPorChave();
            var retencaoDias = config?.RetencaoGravacoesTentativasDias ?? 90;

            // T50M envia o CodigoUsuario (6 dígitos do pool) como UserCode/PessoaID.
            // Procura primeiro por CodigoUsuario; fallback para Pessoa.Id (compatibilidade com cadastros antigos sem CodigoUsuario).
            var pessoa = await pessoaRepo.BuscarPorCodigoUsuario(evento.PessoaID.ToString())
                          ?? await pessoaRepo.BuscarPorId(evento.PessoaID);

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

            // HW-16 / doc_tecnica §5.11 — aguarda gravação só em entradas liberadas
            if (acessoLiberado)
            {
                var cameraService = scope.ServiceProvider.GetService<CameraService>();
                if (cameraService != null)
                {
                    var gravacaoPath = await cameraService.GravarTrechoRTSP(
                        ambiente.Id, evento.DataHora, ambiente.TempoEsperaGravacaoSeg);
                    if (gravacaoPath != null)
                    {
                        tentativa.GravacaoPath = gravacaoPath;
                        tentativaRepo.Atualizar(tentativa);
                        Console.WriteLine($"[SimuladorBanco] Gravação associada — {gravacaoPath}");
                    }
                }
            }
        }
    }
}
