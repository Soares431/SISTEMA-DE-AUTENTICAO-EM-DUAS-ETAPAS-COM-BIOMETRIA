namespace BiometricAcess.Worker.HardwareNosso;

// Mensagens que chegam DO Arduino para o C#
public static class Eventos
{
    public const string Pronto = "EVT|READY";
    public const string Auth = "EVT|AUTH";              // legado — não usado no real
    public const string Id = "EVT|ID";               // EVT|ID|100001
    public const string Senha = "EVT|SENHA";            // EVT|SENHA|100001|123456
    public const string DigitalOk = "EVT|FINGER|OK";        // EVT|FINGER|OK|100001
    public const string DigitalFalhou = "EVT|FINGER|FAIL";
    public const string DigitalCadastrada = "EVT|FINGER|ENROLLED"; // EVT|FINGER|ENROLLED|100001
    public const string DigoPosto = "EVT|FINGER|PLACED";
    public const string DigoRetirado = "EVT|FINGER|REMOVED";
    // Heartbeat do sensor AS608 — Arduino emite a cada INTERVALO_HEARTBEAT_MS
    public const string SensorOk = "EVT|FINGER|SENSOR|OK";
    public const string SensorFalhou = "EVT|FINGER|SENSOR|FAIL";
    // EVT|FINGER|DELETED|<slot> — confirmação de deleção do template
    public const string DigitalApagada = "EVT|FINGER|DELETED";
    // EVT|SERVIDOR|TIMEOUT — Arduino não recebeu resposta do Worker no tempo esperado
    public const string ServidorTimeout = "EVT|SERVIDOR|TIMEOUT";
}

// Comandos que o C# manda PARA o Arduino
public static class Comandos
{
    public const string LcdLinha1 = "CMD|LCD|LINE1";
    public const string LcdLinha2 = "CMD|LCD|LINE2";
    public const string LcdLimpar = "CMD|LCD|CLEAR";
    public const string PedirSenha = "CMD|ASK|PASSWORD";     // C# pede senha ao Arduino
    public const string DigitalCapturar = "CMD|FINGER|START_ENROLL";
    public const string DigitalVerificar = "CMD|FINGER|START_VERIFY";
    public const string DigitalCancelar = "CMD|FINGER|CANCEL";
    public const string BuzzerOk = "CMD|BUZZER|OK";
    public const string BuzzerFalhou = "CMD|BUZZER|FAIL";
    public const string AccessDenied = "CMD|ACCESS|DENIED";
    // CMD|RELAY|OPEN|<segundos> — aciona relé/solenoide da fechadura pelo tempo informado
    public const string RelayOpen = "CMD|RELAY|OPEN";
    // CMD|FINGER|DELETE|<slot> — apaga o template do slot no AS608
    public const string DigitalApagar = "CMD|FINGER|DELETE";
}

// Parser — quebra uma linha recebida em partes
public class MensagemSerial
{
    public string Tipo { get; private set; } = "";
    public string Modulo { get; private set; } = "";
    public string Acao { get; private set; } = "";
    public string Dado { get; private set; } = "";
    public string Dado2 { get; private set; } = "";

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
            Dado2 = partes.Length > 4 ? partes[4] : ""
        };
    }

    public bool EhEvento(string eventoConstante)
    {
        var prefixo = $"{Tipo}|{Modulo}|{Acao}";
        return prefixo.StartsWith(eventoConstante);
    }
}