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
    private string _digitandoId = "";

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

            Console.WriteLine($"Arduino conectado em {_porta} a {_baudRate} baud");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao conectar Arduino: {ex.Message}");
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

            // Tecla pressionada — vai montando o ID digitado
            if (msg.EhEvento(Eventos.TeclaPressionada))
            {
                var tecla = msg.Dado;

                if (tecla == "#")
                {
                    // # cancela o que estava digitando
                    _digitandoId = "";
                    EnviarComando($"{Comandos.LcdLinha1}|Cancelado");
                    EnviarComando($"{Comandos.LcdLinha2}|Digite o ID:");
                    return;
                }

                if (tecla == "*")
                {
                    // * confirma o ID — dispara o fluxo de acesso
                    ProcessarId(_digitandoId);
                    _digitandoId = "";
                    return;
                }

                // Qualquer outra tecla acumula o ID
                _digitandoId += tecla;
                EnviarComando($"{Comandos.LcdLinha2}|ID: {_digitandoId}");
                return;
            }

            // Digital reconhecida
            if (msg.EhEvento(Eventos.DigitalOk))
            {
                _ultimoEvento = new EventoAcesso
                {
                    PessoaID = int.TryParse(msg.Dado, out var id) ? id : 0,
                    TipoVerificacao = "digital_id",
                    AcessoLiberado = true,
                    DataHora = DateTime.Now,
                    IpDispositivo = _porta,
                    MotivoNegacao = string.Empty
                };

                EnviarComando(Comandos.BuzzerOk);
                return;
            }

            // Digital falhou
            if (msg.EhEvento(Eventos.DigitalFalhou))
            {
                EnviarComando(Comandos.BuzzerFalhou);
                EnviarComando($"{Comandos.LcdLinha1}|Nao reconhecido");
                return;
            }

            // Arduino inicializado
            if (msg.EhEvento(Eventos.Pronto))
            {
                EnviarComando($"{Comandos.LcdLinha1}|Sistema Pronto");
                EnviarComando($"{Comandos.LcdLinha2}|Digite o ID:");
            }
        }
        catch (TimeoutException) { }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar mensagem serial: {ex.Message}");
        }
    }

    private void ProcessarId(string idDigitado)
    {
        if (!int.TryParse(idDigitado, out var pessoaId))
        {
            EnviarComando($"{Comandos.LcdLinha1}|ID invalido");
            return;
        }

        // Pede verificação da digital
        EnviarComando($"{Comandos.LcdLinha1}|ID: {pessoaId}");
        EnviarComando($"{Comandos.LcdLinha2}|Coloque o dedo");
        EnviarComando(Comandos.DigitalVerificar);
    }

    public void EnviarComando(string comando)
    {
        try
        {
            _serial?.WriteLine(comando);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao enviar comando: {ex.Message}");
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
        Console.WriteLine("Arduino desconectado");
    }
}