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
            var pessoaT50Repo       = scope.ServiceProvider.GetRequiredService<IPessoaT50Repository>();

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

            // Multi-T50: um T50 pode estar vinculado a múltiplos ambientes. Pega o primeiro vinculado.
            // FALLBACK: se a tabela ambienteT50 estiver vazia (migração incompleta), usa
            // Ambiente.DispositivoT50Id como antes — assim o simulador nunca para por falta de dados.
            var ambientesDoT50 = ambienteT50Repo.ListarAmbientesDoT50(dispositivo.Id);
            var ambiente       = ambientesDoT50.FirstOrDefault();
            if (ambiente == null)
            {
                ambiente = ambienteRepo.ListarTodos().FirstOrDefault(a => a.DispositivoT50Id == dispositivo.Id);
                if (ambiente != null)
                {
                    Console.WriteLine($"[SimuladorBanco] AVISO: usando fallback Ambiente.DispositivoT50Id (tabela ambienteT50 vazia para T50 {dispositivo.Id}).");
                }
            }

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
            else if (evento.TipoVerificacao == "digital_id"
                  && pessoa.modoAcesso == "digital_e_senha"
                  && !pessoaT50Repo.EstaCadastrada(pessoa.Id, dispositivo.Id))
            {
                // Pessoa tem acesso ao ambiente, MAS a biometria dela não está cadastrada NESTE T50.
                // Só nega quando o evento é "digital_id" — se for "senha_id" não precisa do template.
                // Acontece quando o ambiente tem múltiplos T50 e admin escolheu não cadastrar a digital em todos.
                acessoLiberado = false;
                motivoNegacao  = "biometria_nao_cadastrada_neste_t50";
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

            // doc_tecnica §5.11 — TODA tentativa (liberada ou negada) gera gravação,
            // porque o objetivo é registrar ATIVIDADE no ambiente, não só sucessos.
            var cameraService = scope.ServiceProvider.GetService<CameraService>();
            if (cameraService == null)
            {
                Console.WriteLine($"[SimuladorBanco] AVISO: CameraService não registrado no DI — gravação pulada");
            }
            else
            {
                var cameraRepo = scope.ServiceProvider.GetRequiredService<ICameraRepository>();
                var cams = await cameraRepo.ListarPorAmbiente(ambiente.Id);
                var camRtsp = cams.FirstOrDefault(c => c.Ativa && !string.IsNullOrWhiteSpace(c.UrlRTSP));
                if (camRtsp == null)
                {
                    Console.WriteLine($"[SimuladorBanco] Ambiente {ambiente.Nome} não tem câmera ativa com UrlRTSP cadastrada — sem gravação.");
                }
                else
                {
                    Console.WriteLine($"[SimuladorBanco] Iniciando gravação ({ambiente.TempoEsperaGravacaoSeg}s) — câmera '{camRtsp.Nome}' em {camRtsp.UrlRTSP}");
                    var gravacaoPath = await cameraService.GravarTrechoRTSP(
                        ambiente.Id, evento.DataHora, ambiente.TempoEsperaGravacaoSeg);
                    if (gravacaoPath != null)
                    {
                        tentativa.GravacaoPath = gravacaoPath;
                        tentativaRepo.Atualizar(tentativa);
                        Console.WriteLine($"[SimuladorBanco] Gravação associada — {gravacaoPath}");
                    }
                    else
                    {
                        Console.WriteLine($"[SimuladorBanco] Gravação FALHOU — verifique se a URL RTSP '{camRtsp.UrlRTSP}' está acessível.");
                    }
                }
            }
        }
    }
}
