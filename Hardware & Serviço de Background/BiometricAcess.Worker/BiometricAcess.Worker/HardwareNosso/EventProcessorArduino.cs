using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;
using InfraestruturaBloco1.Services;
using Microsoft.Extensions.Configuration;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.HardwareNosso;

public class EventProcessorArduino : IEventProcessor
{
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IAmbientePessoaRepository _ambientePessoaRepository;
    private readonly IDispositivoT50Repository _dispositivoRepository;
    private readonly IAmbienteRepository _ambienteRepository;
    private readonly ITentativaAcessoRepository _tentativaRepository;
    private readonly IConfiguracaoRepository _configuracaoRepository;
    private readonly IAnvizArduinoService _arduinoService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _aesKey;

    public EventProcessorArduino(
        IPessoaRepository pessoaRepository,
        IAmbientePessoaRepository ambientePessoaRepository,
        IDispositivoT50Repository dispositivoRepository,
        IAmbienteRepository ambienteRepository,
        ITentativaAcessoRepository tentativaRepository,
        IConfiguracaoRepository configuracaoRepository,
        IAnvizArduinoService arduinoService,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory)
    {
        _pessoaRepository = pessoaRepository;
        _ambientePessoaRepository = ambientePessoaRepository;
        _dispositivoRepository = dispositivoRepository;
        _ambienteRepository = ambienteRepository;
        _tentativaRepository = tentativaRepository;
        _configuracaoRepository = configuracaoRepository;
        _arduinoService = arduinoService;
        _scopeFactory = scopeFactory;
        _aesKey = AesHelper.ResolverChave(configuration);
    }

    public async Task Processar(EventoAcesso evento)
    {
        var dispositivo = BuscarDispositivoPorIp(evento.IpDispositivo);
        if (dispositivo == null)
        {
            Console.WriteLine($"[Arduino] Dispositivo não encontrado para porta: {evento.IpDispositivo}");
            return;
        }

        // Bug C2 (paralelo ao corrigido em EventProcessor.cs): resolver o ambiente pelo DispositivoT50Id,
        // não usar dispositivo.Id como ambienteId.
        var ambiente = _ambienteRepository.ListarTodos()
            .FirstOrDefault(a => a.DispositivoT50Id == dispositivo.Id);
        if (ambiente == null)
        {
            Console.WriteLine($"[Arduino] Nenhum ambiente configurado para o dispositivo {dispositivo.Id}");
            return;
        }

        // Heartbeat de status online/offline
        _dispositivoRepository.RegistrarHeartbeat(dispositivo.EnderecoIP);

        // Arduino envia o CodigoUsuario (6 dígitos) — busca por código primeiro, fallback no Id legado
        var pessoa = await _pessoaRepository.BuscarPorCodigoUsuario(evento.PessoaID.ToString())
                      ?? await _pessoaRepository.BuscarPorId(evento.PessoaID);

        // ── EVT|ID — Arduino mandou só o ID ──────────────────────────
        // C# decide se pede senha (primeiro acesso) ou digital (já tem biometria)
        if (evento.TipoVerificacao == "id")
        {
            if (pessoa == null)
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "nao_cadastrado");
                await RegistrarTentativa(evento, null, ambiente.Id, false, "nao_cadastrado");
                return;
            }

            if (pessoa.Status == "inativo")
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "inativo");
                await RegistrarTentativa(evento, pessoa, ambiente.Id, false, "inativo");
                return;
            }

            if (!_ambientePessoaRepository.PessoaTemAcesso(ambiente.Id, pessoa.Id))
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "sem_permissao");
                await RegistrarTentativa(evento, pessoa, ambiente.Id, false, "sem_permissao");
                return;
            }

            if (pessoa.biometriaCadastrada == null)
            {
                // Primeiro acesso — pede senha
                _arduinoService.NotificarPedirSenha(evento.PessoaID);
                return;
            }

            // Já tem biometria — pede digital direto
            _arduinoService.NotificarVerificarDigital(evento.PessoaID);
            return;
        }

        // ── EVT|SENHA — Arduino mandou senha após pedido do C# ───────
        // C# valida senha e decide se cadastra digital
        if (evento.TipoVerificacao == "senha")
        {
            if (pessoa == null)
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "nao_cadastrado");
                return;
            }

            // Bug fix: verifica status inativo também no fluxo de senha (antes só verificava em EVT|ID).
            if (pessoa.Status == "inativo")
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "inativo");
                await RegistrarTentativa(evento, pessoa, ambiente.Id, false, "inativo");
                return;
            }

            if (!_ambientePessoaRepository.PessoaTemAcesso(ambiente.Id, pessoa.Id))
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "sem_permissao");
                await RegistrarTentativa(evento, pessoa, ambiente.Id, false, "sem_permissao");
                return;
            }

            // Bug fix crítico: senhaClear está armazenada cifrada com AES. Antes a comparação direta
            // `evento.MotivoNegacao != pessoa.senhaClear` SEMPRE retornava true (sempre nega).
            // MotivoNegacao carrega a senha em texto claro digitada pelo usuário.
            string senhaArmazenadaPlain;
            try
            {
                senhaArmazenadaPlain = AesHelper.Decrypt(pessoa.senhaClear, _aesKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Arduino] Falha ao descriptografar senha da pessoa {pessoa.Id}: {ex.Message}");
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "erro_interno");
                await RegistrarTentativa(evento, pessoa, ambiente.Id, false, "erro_interno");
                return;
            }

            if (evento.MotivoNegacao != senhaArmazenadaPlain)
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "senha_invalida");
                await RegistrarTentativa(evento, pessoa, ambiente.Id, false, "senha_invalida");
                return;
            }

            // Senha correta — bifurca pelo modo de acesso da pessoa:
            // - somente_senha: libera direto, sem pedir biometria
            // - digital_e_senha: cadastra biometria no AS608 (1º acesso)
            if (pessoa.modoAcesso == "somente_senha")
            {
                await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);
                var tentSenha = await RegistrarTentativa(evento, pessoa, ambiente.Id, true, null);
                _arduinoService.NotificarAcessoLiberado();
                AgendarGravacaoOnvif(tentSenha.Id, ambiente);
                Console.WriteLine($"[Arduino] Acesso liberado (somente_senha) — Pessoa: {pessoa.Id}");
                return;
            }

            // digital_e_senha — vai cadastrar biometria (1º acesso).
            // Aloca slot do pool 1-127 (limite físico do AS608) via banco. Reuso de slots
            // vazios garante que sensor não lota mesmo após admin resetar biometrias.
            int? slot = pessoa.SlotAs608;
            if (slot == null)
            {
                slot = await _pessoaRepository.AlocarSlotAs608Livre();
                if (slot == null)
                {
                    Console.WriteLine($"[Arduino] AS608 lotado (127/127) — não foi possível alocar slot pra pessoa {pessoa.Id}");
                    _arduinoService.NotificarAcessoNegado(evento.PessoaID, "sensor_lotado");
                    await RegistrarTentativa(evento, pessoa, ambiente.Id, false, "sensor_lotado");
                    return;
                }
                await _pessoaRepository.DefinirSlotAs608(pessoa.Id, slot.Value);
            }
            _arduinoService.NotificarPrimeiroAcesso(evento.PessoaID, slot.Value);
            return;
        }

        // ── Digital ou primeiro acesso concluído ─────────────────────
        if (pessoa == null) return;

        if (evento.TipoVerificacao == "primeiro_acesso")
        {
            await _pessoaRepository.MarcarBiometriaCadastrada(pessoa.Id);
            // doc_tecnica §2.3: incrementa contador de digitais do T50 quando enroll confirma.
            if (dispositivo.DigitaisCadastradas < 1000)
            {
                dispositivo.DigitaisCadastradas++;
                _dispositivoRepository.Atualizar(dispositivo);
            }
            Console.WriteLine($"[Arduino] Biometria cadastrada — Pessoa: {pessoa.Id}");
        }

        await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);
        var tentativa = await RegistrarTentativa(evento, pessoa, ambiente.Id, true, null);
        // Centraliza abertura da porta: buzzer OK + relé pelo tempo padrão.
        // (Antes o BUZZER|OK saía direto do ArduinoConnector — agora EventProcessor controla.)
        _arduinoService.NotificarAcessoLiberado();
        AgendarGravacaoOnvif(tentativa.Id, ambiente);
    }

    // §5.11 doc técnica: após acesso liberado, aguarda em background a câmera capturar
    // o movimento via ONVIF e persiste a URL da gravação na TentativaAcesso.
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
                Console.WriteLine($"[ONVIF Arduino] Falha ao associar gravação à tentativa {tentativaId}: {ex.Message}");
            }
        });
    }

    private DispositivoT50? BuscarDispositivoPorIp(string ip)
    {
        return _dispositivoRepository.ListarTodos()
            .FirstOrDefault(d => d.EnderecoIP == ip);
    }

    private async Task<TentativaAcesso> RegistrarTentativa(EventoAcesso evento, Pessoa? pessoa, int ambienteId, bool acessoLiberado, string? motivo)
    {
        // Bug fix: preenche DataExpiracao para o job de limpeza funcionar.
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
}
