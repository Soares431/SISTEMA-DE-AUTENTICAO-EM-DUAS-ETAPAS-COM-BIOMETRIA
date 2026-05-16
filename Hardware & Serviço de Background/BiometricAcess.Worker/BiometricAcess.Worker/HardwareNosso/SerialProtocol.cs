namespace BiometricAcess.Worker.HardwareNosso;

// Mensagens que chegam DO Arduino para o C#
public static class Eventos
{
    public const string TeclaPressionada = "EVT|KEY";
    public const string DigitalOk = "EVT|FINGER|OK";
    public const string DigitalFalhou = "EVT|FINGER|FAIL";
    public const string DigitalCadastrada = "EVT|FINGER|ENROLLED"; // primeiro acesso concluído
    public const string DigoPosto = "EVT|FINGER|PLACED";
    public const string DigoRetirado = "EVT|FINGER|REMOVED";
    public const string Pronto = "EVT|READY";
    public const string Auth = "EVT|AUTH";             // EVT|AUTH|ID|SENHA
}

// Comandos que o C# manda PARA o Arduino
public static class Comandos
{
    public const string LcdLinha1 = "CMD|LCD|LINE1";
    public const string LcdLinha2 = "CMD|LCD|LINE2";
    public const string LcdLimpar = "CMD|LCD|CLEAR";
    public const string DigitalCapturar = "CMD|FINGER|START_ENROLL";
    public const string DigitalVerificar = "CMD|FINGER|START_VERIFY";
    public const string DigitalCancelar = "CMD|FINGER|CANCEL";
    public const string BuzzerOk = "CMD|BUZZER|OK";
    public const string BuzzerFalhou = "CMD|BUZZER|FAIL";
    public const string AccessDenied = "CMD|ACCESS|DENIED";     // CMD|ACCESS|DENIED|motivo
}

// Parser — quebra uma linha recebida em partes
public class MensagemSerial
{
    public string Tipo { get; private set; } = "";
    public string Modulo { get; private set; } = "";
    public string Acao { get; private set; } = "";
    public string Dado { get; private set; } = "";
    public string Dado2 { get; private set; } = "";  // ← novo

    public static MensagemSerial? Parse(string linha)
    {
        var partes = linha.Trim().Split('|');
        if (partes.Length < 3) return null;

        return new MensagemSerial
        {
            Tipo = partes[0],
            Modulo = partes[1],
            Acao = partes[2],
            Dado = partes.Length > 3 ? partes[3] : "",
            Dado2 = partes.Length > 4 ? partes[4] : ""  // ← novo
        };
    }

    public bool EhEvento(string eventoConstante)
    {
        var prefixo = $"{Tipo}|{Modulo}|{Acao}";
        return prefixo.StartsWith(eventoConstante);
    }
}