using BiometricAcess.Worker.Models;
using InfraestruturaBloco1.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.Services
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IPessoaRepository _pessoaRepository;
        private readonly IAmbientePessoaRepository _ambientePessoaRepository;
        private readonly IDispositivoT50Repository _dispositivoRepository;
        private readonly IAmbienteRepository _ambienteRepository;
        private readonly ITentativaAcessoRepository _tentativaRepository;
        private readonly IConfiguracaoRepository _configuracaoRepository;
        private readonly IAnvizService _anvizService;
        private readonly IServiceScopeFactory _scopeFactory;

        public EventProcessor(
            IPessoaRepository pessoaRepository,
            IAmbientePessoaRepository ambientePessoaRepository,
            IDispositivoT50Repository dispositivoRepository,
            IAmbienteRepository ambienteRepository,
            ITentativaAcessoRepository tentativaRepository,
            IConfiguracaoRepository configuracaoRepository,
            IAnvizService anvizService,
            IServiceScopeFactory scopeFactory)
        {
            _pessoaRepository = pessoaRepository;
            _ambientePessoaRepository = ambientePessoaRepository;
            _dispositivoRepository = dispositivoRepository;
            _ambienteRepository = ambienteRepository;
            _tentativaRepository = tentativaRepository;
            _configuracaoRepository = configuracaoRepository;
            _anvizService = anvizService;
            _scopeFactory = scopeFactory;
        }

        public async Task Processar(EventoAcesso evento)
        {
            var dispositivo = BuscarDispositivoPorIp(evento.IpDispositivo);
            if (dispositivo == null)
            {
                Console.WriteLine($"Dispositivo não encontrado para IP: {evento.IpDispositivo}");
                return;
            }

            // Heartbeat de status online/offline
            _dispositivoRepository.RegistrarHeartbeat(dispositivo.EnderecoIP);

            // C2: busca o Ambiente pelo DispositivoT50Id — antes usava o ID do dispositivo como ambienteId
            var ambiente = _ambienteRepository.ListarTodos()
                .FirstOrDefault(a => a.DispositivoT50Id == dispositivo.Id);
            if (ambiente == null)
            {
                Console.WriteLine($"Nenhum ambiente configurado para o dispositivo {dispositivo.Id}");
                return;
            }

            // T50M envia o CodigoUsuario (6 dígitos) — busca por código primeiro, fallback no Id legado
            var pessoa = await _pessoaRepository.BuscarPorCodigoUsuario(evento.PessoaID.ToString())
                          ?? await _pessoaRepository.BuscarPorId(evento.PessoaID);

            if (pessoa == null)
            {
                await FluxoNaoCadastrado(evento, ambiente.Id);
                return;
            }

            if (pessoa.Status == "inativo")
            {
                await FluxoAcessoNegado(evento, pessoa, ambiente.Id, "inativo");
                return;
            }

            if (!_ambientePessoaRepository.PessoaTemAcesso(ambiente.Id, pessoa.Id))
            {
                await FluxoAcessoNegado(evento, pessoa, ambiente.Id, "sem_permissao");
                return;
            }

            if (evento.TipoVerificacao == "senha_id"
                && pessoa.modoAcesso == "digital_e_senha"
                && pessoa.biometriaCadastrada == null)
            {
                await FluxoPrimeiroAcesso(evento, pessoa, ambiente);
                return;
            }

            await FluxoAcessoNormal(evento, pessoa, ambiente);
        }

        private DispositivoT50? BuscarDispositivoPorIp(string ip)
        {
            return _dispositivoRepository.ListarTodos()
                .FirstOrDefault(d => d.EnderecoIP == ip);
        }

        private async Task FluxoPrimeiroAcesso(EventoAcesso evento, Pessoa pessoa, Ambiente ambiente)
        {
            Console.WriteLine($"Primeiro acesso — Pessoa: {pessoa.Id} ({pessoa.CodigoUsuario}) | Iniciando captura de digital...");

            // Comunicação com T50M usa CodigoUsuario (EmployeeId 6 dígitos do pool)
            var codigoT50 = CodigoT50DePessoa(pessoa);

            // I1: EnrollFingerprint retorna o template diretamente — elimina chamada redundante ao DownloadTemplate
            var template = _anvizService.IniciarCapturaDigital(codigoT50);
            if (template != null)
            {
                await _pessoaRepository.SalvarTemplate(pessoa.Id, template);
                await _pessoaRepository.MarcarBiometriaCadastrada(pessoa.Id);
                // Bug 4: "ambos" → Mode=6 (FP|PWD bitmask) — usuário pode escolher digital OU senha
                // toda vez, conforme doc §2.2. Mode 6 também era o valor de "digital_id"; mantido
                // como alias pra não quebrar chamadas existentes.
                _anvizService.AlterarModo(codigoT50, "ambos");
                Console.WriteLine($"Biometria cadastrada — Pessoa: {pessoa.Id}");
            }

            await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);
            var tentativa = await RegistrarTentativa(evento, pessoa, ambiente.Id, true, null);
            AgendarGravacaoOnvif(tentativa.Id, ambiente);
        }

        private async Task FluxoAcessoNormal(EventoAcesso evento, Pessoa pessoa, Ambiente ambiente)
        {
            Console.WriteLine($"Acesso liberado — Pessoa: {pessoa.Id} | Tipo: {evento.TipoVerificacao}");
            await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);

            var tentativa = await RegistrarTentativa(evento, pessoa, ambiente.Id, true, null);
            AgendarGravacaoOnvif(tentativa.Id, ambiente);
        }

        // §5.11 doc técnica: após acesso liberado, aguarda em background a câmera capturar
        // o movimento via ONVIF e persiste a URL da gravação na TentativaAcesso.
        // Fire-and-forget porque o polling de eventos não pode travar 30-120s por gravação.
        private void AgendarGravacaoOnvif(int tentativaId, Ambiente ambiente)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var cameraService = scope.ServiceProvider.GetRequiredService<CameraService>();
                    var tentativaRepo = scope.ServiceProvider.GetRequiredService<ITentativaAcessoRepository>();
                    var url = await cameraService.MonitorarNovoArquivo(ambiente.Id, DateTime.UtcNow, ambiente.TempoEsperaGravacaoSeg);
                    if (!string.IsNullOrEmpty(url))
                        tentativaRepo.AtualizarGravacaoPath(tentativaId, url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ONVIF] Falha ao associar gravação à tentativa {tentativaId}: {ex.Message}");
                }
            });
        }

        private async Task FluxoNaoCadastrado(EventoAcesso evento, int ambienteId)
        {
            Console.WriteLine($"Acesso negado — Pessoa {evento.PessoaID} não cadastrada no sistema");
            await RegistrarTentativa(evento, null, ambienteId, false, "nao_cadastrado");
        }

        private async Task FluxoAcessoNegado(EventoAcesso evento, Pessoa pessoa, int ambienteId, string motivo)
        {
            Console.WriteLine($"Acesso negado — Pessoa: {pessoa.Id} | Motivo: {motivo}");
            await RegistrarTentativa(evento, pessoa, ambienteId, false, motivo);
        }

        private async Task<TentativaAcesso> RegistrarTentativa(EventoAcesso evento, Pessoa? pessoa, int ambienteId, bool acessoLiberado, string? motivo)
        {
            // C4: preenche DataExpiracao com base na configuração (antes ficava null, job de limpeza nunca removia)
            var config = await _configuracaoRepository.BuscarPorChave();
            var retencaoDias = config?.RetencaoGravacoesTentativasDias ?? 90;

            var tentativa = new TentativaAcesso
            {
                PessoaId = pessoa?.Id,
                AmbienteId = ambienteId,
                DataHora = evento.DataHora,
                AcessoLiberado = acessoLiberado,
                MotivoNegacao = motivo,
                TipoVerificacao = evento.TipoVerificacao,
                DataExpiracao = DateTime.UtcNow.AddDays(retencaoDias)
            };

            _tentativaRepository.Registrar(tentativa);
            return tentativa;
        }

        // Converte Pessoa em ID a ser enviado ao T50M. Prefere CodigoUsuario (6 dígitos do pool).
        // Fallback para Pessoa.Id se ainda não migrado (compat. cadastros legados).
        private static int CodigoT50DePessoa(Pessoa pessoa)
        {
            if (!string.IsNullOrEmpty(pessoa.CodigoUsuario) && int.TryParse(pessoa.CodigoUsuario, out var c))
                return c;
            return (int)pessoa.Id;
        }
    }
}
