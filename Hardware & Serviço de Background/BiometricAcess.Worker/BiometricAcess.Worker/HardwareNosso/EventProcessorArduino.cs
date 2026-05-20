using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;
using WebAbil8_Sistema_Verificação_dupla.slnx.Model;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.HardwareNosso;

public class EventProcessorArduino : IEventProcessor
{
    private readonly IPessoaRepository _pessoaRepository;
    private readonly IAmbientePessoaRepository _ambientePessoaRepository;
    private readonly IDispositivoT50Repository _dispositivoRepository;
    private readonly ITentativaAcessoRepository _tentativaRepository;
    private readonly IAnvizArduinoService _arduinoService;

    public EventProcessorArduino(
        IPessoaRepository pessoaRepository,
        IAmbientePessoaRepository ambientePessoaRepository,
        IDispositivoT50Repository dispositivoRepository,
        ITentativaAcessoRepository tentativaRepository,
        IAnvizArduinoService arduinoService)
    {
        _pessoaRepository = pessoaRepository;
        _ambientePessoaRepository = ambientePessoaRepository;
        _dispositivoRepository = dispositivoRepository;
        _tentativaRepository = tentativaRepository;
        _arduinoService = arduinoService;
    }

    public async Task Processar(EventoAcesso evento)
    {
        var dispositivo = BuscarDispositivoPorIp(evento.IpDispositivo);
        if (dispositivo == null)
        {
            Console.WriteLine($"[Arduino] Dispositivo não encontrado para porta: {evento.IpDispositivo}");
            return;
        }

        var ambienteId = dispositivo.Id;
        var pessoa = await _pessoaRepository.BuscarPorId(evento.PessoaID);

        // ── EVT|ID — Arduino mandou só o ID ──────────────────────────
        // C# decide se pede senha (primeiro acesso) ou digital (já tem biometria)
        if (evento.TipoVerificacao == "id")
        {
            if (pessoa == null)
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "nao_cadastrado");
                RegistrarTentativa(evento, null, ambienteId, false, "nao_cadastrado");
                return;
            }

            if (pessoa.Status == "inativo")
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "inativo");
                RegistrarTentativa(evento, pessoa, ambienteId, false, "inativo");
                return;
            }

            if (!_ambientePessoaRepository.PessoaTemAcesso(ambienteId, pessoa.Id))
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "sem_permissao");
                RegistrarTentativa(evento, pessoa, ambienteId, false, "sem_permissao");
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

            // MotivoNegacao carrega a senha temporariamente
            if (evento.MotivoNegacao != pessoa.senhaClear)
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "senha_incorreta");
                RegistrarTentativa(evento, pessoa, ambienteId, false, "senha_incorreta");
                return;
            }

            // Senha correta — cadastra biometria
            _arduinoService.NotificarPrimeiroAcesso(evento.PessoaID);
            return;
        }

        // ── Digital ou primeiro acesso concluído ─────────────────────
        if (pessoa == null) return;

        if (evento.TipoVerificacao == "primeiro_acesso")
        {
            await _pessoaRepository.MarcarBiometriaCadastrada(pessoa.Id);
            Console.WriteLine($"[Arduino] Biometria cadastrada — Pessoa: {pessoa.Id}");
        }

        await _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);
        RegistrarTentativa(evento, pessoa, ambienteId, true, null);
    }

    private DispositivoT50 BuscarDispositivoPorIp(string ip)
    {
        var dispositivos = _dispositivoRepository.ListarTodos();
        foreach (var dispositivo in dispositivos)
        {
            if (dispositivo.EnderecoIP == ip)
                return dispositivo;
        }
        return null;
    }

    private void RegistrarTentativa(EventoAcesso evento, Pessoa pessoa, int ambienteId, bool acessoLiberado, string motivo)
    {
        var tentativa = new TentativaAcesso
        {
            PessoaId = pessoa != null ? (int)pessoa.Id : null,
            AmbienteId = ambienteId,
            DataHora = evento.DataHora,
            AcessoLiberado = acessoLiberado,
            MotivoNegacao = motivo,
            TipoVerificacao = evento.TipoVerificacao
        };

        _tentativaRepository.Registrar(tentativa);
    }
}