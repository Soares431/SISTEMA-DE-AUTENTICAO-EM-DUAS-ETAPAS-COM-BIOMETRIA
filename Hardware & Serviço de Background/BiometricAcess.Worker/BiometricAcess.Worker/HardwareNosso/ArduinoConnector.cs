using System.IO.Ports;
using BiometricAcess.Worker.Models;
using BiometricAcess.Worker.Services;
using Microsoft.Extensions.DependencyInjection;
using WebAbil8_Sistema_Verificação_dupla.slnx.Services;

namespace BiometricAcess.Worker.HardwareNosso;

public class ArduinoConnector : IAnvizConnector
{
    private SerialPort? _serial;
    private readonly string _porta;
    private readonly int _baudRate;
    private EventoAcesso? _ultimoEvento;
    private int _idEmAndamento = 0;
    private string _senhaEmAndamento = "";
    private bool _sensorOnline = true;
    private DateTime _ultimoHeartbeat = DateTime.UtcNow;
    // ScopeFactory opcional pra registrar heartbeat de DB direto do EVT|FINGER|SENSOR|OK.
    // Sem isto, o status online dependia só do polling do Worker (60s) — qualquer hiccup
    // no Worker (reconexão, GC, etc) podia derrubar o dispositivo pra "offline" no painel.
    public IServiceScopeFactory? ScopeFactory { get; set; }

    public bool SensorOnline => _sensorOnline;
    public DateTime UltimoHeartbeat => _ultimoHeartbeat;

    public ArduinoConnector(string porta, int baudRate = 9600)
    {
        _porta = porta;
        _baudRate = baudRate;
    }

    public string EnderecoIdentificador => _porta;

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

            // ── EVT|READY ────────────────────────────────────────────
            if (msg.EhEvento(Eventos.Pronto))
            {
                EnviarComando($"{Comandos.LcdLinha1}|Sistema Pronto");
                EnviarComando($"{Comandos.LcdLinha2}|Digite o ID:");
                return;
            }

            // ── EVT|ID|100001 ─────────────────────────────────────────
            // Arduino digitou ID — C# decide se pede senha ou digital
            if (msg.EhEvento(Eventos.Id))
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
                    TipoVerificacao = "id",
                    AcessoLiberado = false,
                    DataHora = DateTime.Now,
                    IpDispositivo = _porta,
                    MotivoNegacao = string.Empty
                };
                return;
            }

            // ── EVT|SENHA|100001|123456 ───────────────────────────────
            // Arduino digitou senha — C# valida e decide próximo passo
            if (msg.EhEvento(Eventos.Senha))
            {
                if (!int.TryParse(msg.Acao, out var pessoaId))
                {
                    EnviarComando($"{Comandos.AccessDenied}|ID invalido");
                    return;
                }

                _idEmAndamento = pessoaId;
                _senhaEmAndamento = msg.Dado;

                _ultimoEvento = new EventoAcesso
                {
                    PessoaID = pessoaId,
                    TipoVerificacao = "senha",
                    AcessoLiberado = false,
                    DataHora = DateTime.Now,
                    IpDispositivo = _porta,
                    MotivoNegacao = msg.Dado  // senha temporariamente aqui
                };
                return;
            }

            // ── EVT|AUTH|ID|SENHA (legado) ────────────────────────────
            if (msg.EhEvento(Eventos.Auth))
            {
                if (!int.TryParse(msg.Acao, out var pessoaId))
                {
                    EnviarComando($"{Comandos.AccessDenied}|ID invalido");
                    return;
                }

                _idEmAndamento = pessoaId;
                _senhaEmAndamento = msg.Dado;

                _ultimoEvento = new EventoAcesso
                {
                    PessoaID = pessoaId,
                    TipoVerificacao = "senha",
                    AcessoLiberado = false,
                    DataHora = DateTime.Now,
                    IpDispositivo = _porta,
                    MotivoNegacao = msg.Dado
                };
                return;
            }

            // ── EVT|FINGER|OK|ID ─────────────────────────────────────
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

                // BUZZER|OK + RELAY|OPEN são disparados pelo EventProcessorArduino após
                // confirmar a tentativa no banco — não aqui no connector. Garante que a porta
                // só abre se o C# validar permissão (single source of truth).
                _idEmAndamento = 0;
                _senhaEmAndamento = "";
                return;
            }

            // ── EVT|FINGER|FAIL ──────────────────────────────────────
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
                _senhaEmAndamento = "";
                return;
            }

            // ── EVT|FINGER|ENROLLED|ID ───────────────────────────────
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
                _senhaEmAndamento = "";
                return;
            }

            // ── EVT|FINGER|SENSOR|OK|FAIL — heartbeat do AS608 ───────
            // Atualiza estado interno + registra heartbeat no DB (mantém UltimaConexao fresca
            // mesmo sem polling do Worker). FAIL repetido sinaliza problema no sensor.
            if (msg.EhEvento(Eventos.SensorOk))
            {
                _sensorOnline = true;
                _ultimoHeartbeat = DateTime.UtcNow;
                RegistrarHeartbeatDb();
                return;
            }
            if (msg.EhEvento(Eventos.SensorFalhou))
            {
                _sensorOnline = false;
                _ultimoHeartbeat = DateTime.UtcNow;
                // Arduino ainda está respondendo, sensor é que falhou — DB continua online.
                RegistrarHeartbeatDb();
                Console.WriteLine("[Arduino] ALERTA — AS608 não responde (verifique fiação 3.3V/TX/RX)");
                return;
            }

            // ── EVT|FINGER|DELETED|slot — confirmação de DELETE ──────
            if (msg.EhEvento(Eventos.DigitalApagada))
            {
                Console.WriteLine($"[Arduino] Template apagado do AS608 — slot {msg.Dado}");
                return;
            }

            // ── EVT|SERVIDOR|TIMEOUT — Arduino abortou aguardando Worker ─
            if (msg.EhEvento(Eventos.ServidorTimeout))
            {
                Console.WriteLine("[Arduino] Arduino reportou timeout aguardando resposta — fluxo abortado no terminal");
                _idEmAndamento = 0;
                _senhaEmAndamento = "";
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

    private void RegistrarHeartbeatDb()
    {
        if (ScopeFactory == null) return;
        try
        {
            using var scope = ScopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDispositivoT50Repository>();
            repo.RegistrarHeartbeat(_porta);
        }
        catch { }
    }

    public List<EventoAcesso> BuscarEventosArmazenados()
    {
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