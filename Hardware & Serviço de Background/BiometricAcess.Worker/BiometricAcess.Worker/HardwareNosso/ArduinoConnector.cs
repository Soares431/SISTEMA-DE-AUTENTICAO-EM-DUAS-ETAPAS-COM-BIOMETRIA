using System.IO.Ports;
using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker.HardwareNosso;

public class ArduinoConnector : IAnvizConnector
{
    private SerialPort? _serial;
    private readonly string _porta;
    private readonly int _baudRate;
    private EventoAcesso? _ultimoEvento;

    // Guarda o ID do fluxo em andamento para usar quando
    // a digital confirmar (EVT|FINGER|OK chega sem a senha)
    private int _idEmAndamento = 0;

    public ArduinoConnector(string porta, int baudRate = 9600)
    {
        _porta = porta;
        _baudRate = baudRate;
    }

    public bool Conectar()
    {
        try
        {
            _serial = new SerialPort(_porta, _baudRate)
            {
                NewLine = "\n",
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            _serial.DataReceived += OnDadosRecebidos;
            _serial.Open();

            Console.WriteLine($"[Arduino] Conectado em {_porta} a {_baudRate} baud");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Arduino] Erro ao conectar: {ex.Message}");
            return false;
        }
    }

    private void OnDadosRecebidos(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var linha = _serial!.ReadLine();
            var msg = MensagemSerial.Parse(linha);
            if (msg == null) return;

            Console.WriteLine($"[Arduino] Recebido: {linha.Trim()}");

            // ── EVT|READY ───────────────────────────────────────────
            // Arduino inicializou — manda mensagem de boas-vindas
            if (msg.EhEvento(Eventos.Pronto))
            {
                EnviarComando($"{Comandos.LcdLinha1}|Sistema Pronto");
                EnviarComando($"{Comandos.LcdLinha2}|Digite o ID:");
                return;
            }

            // ── EVT|AUTH|ID|SENHA ────────────────────────────────────
            // Arduino digitou ID + senha e quer que o C# decida o fluxo.
            // Aqui o C# só registra o evento de autenticação por senha.
            // O EventProcessor é quem vai consultar o banco e mandar
            // CMD|FINGER|START_VERIFY, CMD|FINGER|START_ENROLL ou
            // CMD|ACCESS|DENIED de volta via ArduinoService.
            if (msg.EhEvento(Eventos.Auth))
            {
                if (!int.TryParse(msg.Acao, out var pessoaId))
                {
                    EnviarComando($"{Comandos.AccessDenied}|ID invalido");
                    return;
                }

                _idEmAndamento = pessoaId;

                _ultimoEvento = new EventoAcesso
                {
                    PessoaID = pessoaId,
                    TipoVerificacao = "senha",
                    AcessoLiberado = false,   // EventProcessor decide isso
                    DataHora = DateTime.Now,
                    IpDispositivo = _porta,
                    MotivoNegacao = string.Empty
                };
                return;
            }

            // ── EVT|FINGER|OK|ID ─────────────────────────────────────
            // Digital confirmada — acesso liberado
            if (msg.EhEvento(Eventos.DigitalOk))
            {
                var id = int.TryParse(msg.Dado, out var fid) ? fid : _idEmAndamento;

                _ultimoEvento = new EventoAcesso
                {
                    PessoaID = id,
                    TipoVerificacao = "digital",
                    AcessoLiberado = true,
                    DataHora = DateTime.Now,
                    IpDispositivo = _porta,
                    MotivoNegacao = string.Empty
                };

                EnviarComando(Comandos.BuzzerOk);
                _idEmAndamento = 0;
                return;
            }

            // ── EVT|FINGER|FAIL ──────────────────────────────────────
            // Digital não reconhecida — acesso negado
            if (msg.EhEvento(Eventos.DigitalFalhou))
            {
                _ultimoEvento = new EventoAcesso
                {
                    PessoaID = _idEmAndamento,
                    TipoVerificacao = "digital",
                    AcessoLiberado = false,
                    DataHora = DateTime.Now,
                    IpDispositivo = _porta,
                    MotivoNegacao = "digital_nao_reconhecida"
                };

                EnviarComando(Comandos.BuzzerFalhou);
                _idEmAndamento = 0;
                return;
            }

            // ── EVT|FINGER|ENROLLED|ID ───────────────────────────────
            // Primeiro acesso — digital cadastrada, libera entrada
            if (msg.EhEvento(Eventos.DigitalCadastrada))
            {
                var id = int.TryParse(msg.Dado, out var eid) ? eid : _idEmAndamento;

                _ultimoEvento = new EventoAcesso
                {
                    PessoaID = id,
                    TipoVerificacao = "primeiro_acesso",
                    AcessoLiberado = true,
                    DataHora = DateTime.Now,
                    IpDispositivo = _porta,
                    MotivoNegacao = string.Empty
                };

                _idEmAndamento = 0;
                return;
            }
        }
        catch (TimeoutException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"[Arduino] Erro ao processar mensagem: {ex.Message}");
        }
    }

    public void EnviarComando(string comando)
    {
        try
        {
            Console.WriteLine($"[Arduino] Enviando: {comando}");
            _serial?.WriteLine(comando);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Arduino] Erro ao enviar comando: {ex.Message}");
        }
    }

    public EventoAcesso? BuscarNovoEvento()
    {
        var evento = _ultimoEvento;
        _ultimoEvento = null;
        return evento;
    }

    public List<EventoAcesso> BuscarEventosArmazenados()
    {
        // Arduino não armazena eventos — só envia em tempo real
        return new List<EventoAcesso>();
    }

    public void Desconectar()
    {
        if (_serial != null)
        {
            _serial.DataReceived -= OnDadosRecebidos;
            _serial.Close();
            _serial = null;
        }
        Console.WriteLine("[Arduino] Desconectado");
    }
}