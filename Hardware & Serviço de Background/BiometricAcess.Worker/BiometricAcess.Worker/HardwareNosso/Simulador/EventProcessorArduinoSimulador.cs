using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker.HardwareNosso.Simulador;

public class EventProcessorArduinoSimulador : IEventProcessor
{
    private readonly IAnvizArduinoService _arduinoService;

    // Pessoas mockadas — edite aqui para testar diferentes cenários
    private readonly List<PessoaMock> _pessoas = new()
    {
        new PessoaMock { Id = 100001, Senha = "123456", Ativa = true,  TemBiometria = false },
        new PessoaMock { Id = 100002, Senha = "654321", Ativa = true,  TemBiometria = true  },
        new PessoaMock { Id = 100003, Senha = "111111", Ativa = false, TemBiometria = false },
        new PessoaMock { Id = 100004, Senha = "999999", Ativa = true,  TemBiometria = true  },
    };

    public EventProcessorArduinoSimulador(IAnvizArduinoService arduinoService)
    {
        _arduinoService = arduinoService;
    }

    public void Processar(EventoAcesso evento)
    {
        Console.WriteLine($"[ArduinoSim] Evento — Pessoa: {evento.PessoaID} | Tipo: {evento.TipoVerificacao}");

        // ── Digital ou primeiro acesso concluído ─────────────────────
        if (evento.TipoVerificacao == "digital" || evento.TipoVerificacao == "primeiro_acesso")
        {
            var pessoaDigital = _pessoas.FirstOrDefault(p => p.Id == evento.PessoaID);
            if (pessoaDigital != null && evento.TipoVerificacao == "primeiro_acesso")
            {
                pessoaDigital.TemBiometria = true;
                Console.WriteLine($"[ArduinoSim] Biometria cadastrada — Pessoa: {evento.PessoaID}");
            }

            Console.WriteLine($"[ArduinoSim] Acesso liberado — Pessoa: {evento.PessoaID}");
            return;
        }

        var pessoa = _pessoas.FirstOrDefault(p => p.Id == evento.PessoaID);

        // ── EVT|ID — Arduino mandou só o ID ──────────────────────────
        if (evento.TipoVerificacao == "id")
        {
            if (pessoa == null)
            {
                Console.WriteLine($"[ArduinoSim] Pessoa {evento.PessoaID} não cadastrada");
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "nao_cadastrado");
                return;
            }

            if (!pessoa.Ativa)
            {
                Console.WriteLine($"[ArduinoSim] Pessoa {evento.PessoaID} inativa");
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "inativo");
                return;
            }

            if (!pessoa.TemBiometria)
            {
                // Primeiro acesso — pede senha
                Console.WriteLine($"[ArduinoSim] Primeiro acesso — pedindo senha — Pessoa: {evento.PessoaID}");
                _arduinoService.NotificarPedirSenha(evento.PessoaID);
                return;
            }

            // Já tem biometria — pede digital direto
            Console.WriteLine($"[ArduinoSim] Solicitando digital — Pessoa: {evento.PessoaID}");
            _arduinoService.NotificarVerificarDigital(evento.PessoaID);
            return;
        }

        // ── EVT|SENHA — Arduino mandou senha após pedido ──────────────
        if (evento.TipoVerificacao == "senha")
        {
            if (pessoa == null)
            {
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "nao_cadastrado");
                return;
            }

            // MotivoNegacao carrega a senha temporariamente
            if (evento.MotivoNegacao != pessoa.Senha)
            {
                Console.WriteLine($"[ArduinoSim] Senha incorreta — Pessoa: {evento.PessoaID}");
                _arduinoService.NotificarAcessoNegado(evento.PessoaID, "senha_incorreta");
                return;
            }

            // Senha correta — cadastra biometria
            Console.WriteLine($"[ArduinoSim] Senha correta — iniciando cadastro de digital — Pessoa: {evento.PessoaID}");
            _arduinoService.NotificarPrimeiroAcesso(evento.PessoaID);
            return;
        }
    }

    private class PessoaMock
    {
        public int Id { get; set; }
        public string Senha { get; set; } = "";
        public bool Ativa { get; set; }
        public bool TemBiometria { get; set; }
    }
}