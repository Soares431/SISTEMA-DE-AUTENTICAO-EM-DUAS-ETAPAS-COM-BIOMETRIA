namespace BiometricAcess.Worker.HardwareNosso;

public class ArduinoServiceExtras : IAnvizArduinoService
{
    private readonly ArduinoConnector _connector;

    public ArduinoServiceExtras(ArduinoConnector connector)
    {
        _connector = connector;
    }

    public void NotificarPrimeiroAcesso(int pessoaId)
    {
        Console.WriteLine($"[ArduinoServiceExtras] Primeiro acesso — enviando START_ENROLL para ID {pessoaId}");
        _connector.EnviarComando($"{Comandos.DigitalCapturar}|{pessoaId}");
    }

    public void NotificarVerificarDigital(int pessoaId)
    {
        Console.WriteLine($"[ArduinoServiceExtras] Verificando digital — enviando START_VERIFY para ID {pessoaId}");
        _connector.EnviarComando(Comandos.DigitalVerificar);
    }

    public void NotificarAcessoNegado(int pessoaId, string motivo)
    {
        Console.WriteLine($"[ArduinoServiceExtras] Acesso negado — Pessoa: {pessoaId} | Motivo: {motivo}");
        _connector.EnviarComando($"{Comandos.AccessDenied}|{motivo}");
    }

    public void NotificarPedirSenha(int pessoaId)
    {
        Console.WriteLine($"[ArduinoServiceExtras] Pedindo senha — ID {pessoaId}");
        _connector.EnviarComando(Comandos.PedirSenha);
    }
}