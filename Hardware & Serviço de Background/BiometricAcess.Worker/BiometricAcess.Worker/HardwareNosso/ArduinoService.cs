using BiometricAcess.Worker.Services;

namespace BiometricAcess.Worker.HardwareNosso;

public class ArduinoService : IAnvizService
{
    private readonly ArduinoConnector _connector;

    public ArduinoService(ArduinoConnector connector)
    {
        _connector = connector;
    }

    public bool AdicionarPessoa(int id, string nome, string senha)
    {
        // Arduino não tem banco interno de usuários
        // O controle fica todo no banco do Int1
        Console.WriteLine($"[ArduinoService] AdicionarPessoa: {id} - {nome}");
        return true;
    }

    public bool RemoverPessoa(int id)
    {
        Console.WriteLine($"[ArduinoService] RemoverPessoa: {id}");
        return true;
    }

    public bool IniciarCapturaDigital(int id)
    {
        _connector.EnviarComando($"{Comandos.DigitalCapturar}|{id}");
        Console.WriteLine($"[ArduinoService] Iniciando captura digital para ID {id}");
        return true;
    }

    public byte[]? DownloadTemplate(int id)
    {
        // AS608 não suporta download de template via serial
        // Template fica armazenado no próprio sensor
        Console.WriteLine($"[ArduinoService] DownloadTemplate não suportado no Arduino");
        return null;
    }

    public bool UploadTemplate(int id, byte[] template)
    {
        // AS608 não suporta upload de template via serial
        Console.WriteLine($"[ArduinoService] UploadTemplate não suportado no Arduino");
        return false;
    }

    public bool AlterarModo(int id, string modo)
    {
        // Arduino não tem conceito de modo por usuário
        Console.WriteLine($"[ArduinoService] AlterarModo ignorado no Arduino");
        return true;
    }

    public bool SincronizarHora()
    {
        // Arduino tem RTC opcional — por ora não implementado
        Console.WriteLine($"[ArduinoService] SincronizarHora não implementado");
        return false;
    }
}