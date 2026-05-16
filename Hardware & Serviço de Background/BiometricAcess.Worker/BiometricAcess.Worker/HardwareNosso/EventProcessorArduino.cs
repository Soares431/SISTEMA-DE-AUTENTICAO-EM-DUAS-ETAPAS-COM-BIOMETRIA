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

    public void Processar(EventoAcesso evento)
    {
        var dispositivo = BuscarDispositivoPorIp(evento.IpDispositivo);
        if (dispositivo == null)
        {
            Console.WriteLine($"[Arduino] Dispositivo não encontrado para porta: {evento.IpDispositivo}");
            return;
        }

        var ambienteId = dispositivo.Id;
        var pessoa = _pessoaRepository.BuscarPorId(evento.PessoaID);

        if (pessoa == null)
        {
            FluxoNaoCadastrado(evento, ambienteId);
            return;
        }

        if (pessoa.Status == "inativo")
        {
            FluxoAcessoNegado(evento, pessoa, ambienteId, "inativo");
            return;
        }

        if (!_ambientePessoaRepository.PessoaTemAcesso(ambienteId, pessoa.Id))
        {
            FluxoAcessoNegado(evento, pessoa, ambienteId, "sem_permissao");
            return;
        }

        // Evento de autenticação por senha — C# decide o próximo passo
        if (evento.TipoVerificacao == "senha")
        {
            if (pessoa.biometriaCadastrada == null)
                FluxoPrimeiroAcesso(evento, pessoa, ambienteId);
            else
                FluxoSolicitarDigital(evento, pessoa, ambienteId);

            return;
        }

        // Evento de digital ou primeiro acesso — acesso já confirmado pelo sensor
        FluxoAcessoNormal(evento, pessoa, ambienteId);
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

    private void FluxoPrimeiroAcesso(EventoAcesso evento, Pessoa pessoa, int ambienteId)
    {
        Console.WriteLine($"[Arduino] Primeiro acesso — Pessoa: {pessoa.Id}");

        _arduinoService.NotificarPrimeiroAcesso((int)pessoa.Id);

        // Biometria será cadastrada quando chegar EVT|FINGER|ENROLLED
        // O ArduinoConnector vai gerar um novo EventoAcesso com TipoVerificacao = "primeiro_acesso"
        // e esse evento vai passar pelo FluxoAcessoNormal
        RegistrarTentativa(evento, pessoa, ambienteId, true, null);
    }

    private void FluxoSolicitarDigital(EventoAcesso evento, Pessoa pessoa, int ambienteId)
    {
        Console.WriteLine($"[Arduino] Solicitando digital — Pessoa: {pessoa.Id}");

        _arduinoService.NotificarVerificarDigital((int)pessoa.Id);

        // Não registra tentativa aqui — só registra quando chegar EVT|FINGER|OK ou FAIL
    }

    private void FluxoAcessoNormal(EventoAcesso evento, Pessoa pessoa, int ambienteId)
    {
        Console.WriteLine($"[Arduino] Acesso liberado — Pessoa: {pessoa.Id} | Tipo: {evento.TipoVerificacao}");

        if (evento.TipoVerificacao == "primeiro_acesso")
        {
            _pessoaRepository.MarcarBiometriaCadastrada(pessoa.Id);
            Console.WriteLine($"[Arduino] Biometria marcada como cadastrada — Pessoa: {pessoa.Id}");
        }

        _pessoaRepository.AtualizarUltimoAcesso(pessoa.Id);
        RegistrarTentativa(evento, pessoa, ambienteId, true, null);
    }

    private void FluxoNaoCadastrado(EventoAcesso evento, int ambienteId)
    {
        Console.WriteLine($"[Arduino] Pessoa {evento.PessoaID} não cadastrada");

        _arduinoService.NotificarAcessoNegado(evento.PessoaID, "nao_cadastrado");
        RegistrarTentativa(evento, null, ambienteId, false, "nao_cadastrado");
    }

    private void FluxoAcessoNegado(EventoAcesso evento, Pessoa pessoa, int ambienteId, string motivo)
    {
        Console.WriteLine($"[Arduino] Acesso negado — Pessoa: {pessoa.Id} | Motivo: {motivo}");

        _arduinoService.NotificarAcessoNegado((int)pessoa.Id, motivo);
        RegistrarTentativa(evento, pessoa, ambienteId, false, motivo);
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