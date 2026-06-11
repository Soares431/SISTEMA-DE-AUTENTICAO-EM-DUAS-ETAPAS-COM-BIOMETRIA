using System.Text.RegularExpressions;

namespace FrontendControleAcesso.Components.Shared;

// Validações e formatações reutilizáveis para os formulários do painel.
public static class FormatHelper
{
    private static readonly Regex _emailRegex =
        new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    // Remove tudo que não for dígito.
    public static string SoDigitos(string? s) =>
        string.IsNullOrEmpty(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

    // CPF válido = 11 dígitos + checksum + não pode ser 11 dígitos iguais.
    public static bool CpfValido(string? cpf)
    {
        var d = SoDigitos(cpf);
        if (d.Length != 11) return false;
        if (d.Distinct().Count() == 1) return false; // "00000000000", "11111111111" etc.

        int Soma(int len, int peso)
        {
            int s = 0;
            for (int i = 0; i < len; i++) s += (d[i] - '0') * (peso - i);
            return s;
        }

        int dig1 = (Soma(9, 10) * 10) % 11; if (dig1 == 10) dig1 = 0;
        if (dig1 != d[9] - '0') return false;
        int dig2 = (Soma(10, 11) * 10) % 11; if (dig2 == 10) dig2 = 0;
        return dig2 == d[10] - '0';
    }

    // Formata CPF: "12345678901" → "123.456.789-01"; outros tamanhos retornam input limpo.
    public static string FormatarCpf(string? cpf)
    {
        var d = SoDigitos(cpf);
        if (d.Length != 11) return d;
        return $"{d.Substring(0,3)}.{d.Substring(3,3)}.{d.Substring(6,3)}-{d.Substring(9,2)}";
    }

    // Telefone: "11987654321" → "(11) 98765-4321"; "1133334444" → "(11) 3333-4444".
    public static string FormatarTelefone(string? telefone)
    {
        var d = SoDigitos(telefone);
        return d.Length switch
        {
            11 => $"({d.Substring(0,2)}) {d.Substring(2,5)}-{d.Substring(7,4)}",
            10 => $"({d.Substring(0,2)}) {d.Substring(2,4)}-{d.Substring(6,4)}",
            _  => d
        };
    }

    public static bool EmailValido(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Length <= 150 && _emailRegex.IsMatch(email);

    // Telefone válido: 10 ou 11 dígitos (com DDD).
    public static bool TelefoneValido(string? telefone)
    {
        var d = SoDigitos(telefone);
        return d.Length == 10 || d.Length == 11;
    }

    // IPv4 válido: 4 octetos 0-255 separados por ponto. Rejeita zeros à esquerda inválidos ("01") e octetos vazios.
    public static bool IpValido(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return false;
        var partes = ip.Split('.');
        if (partes.Length != 4) return false;
        foreach (var p in partes)
        {
            if (p.Length == 0 || p.Length > 3) return false;
            if (!p.All(char.IsDigit)) return false;
            if (!int.TryParse(p, out var oct)) return false;
            if (oct < 0 || oct > 255) return false;
        }
        return true;
    }

    // Porta serial Windows: "COM" + 1..3 dígitos (COM1..COM256). Case-insensitive.
    public static bool PortaSerialValida(string? porta)
    {
        if (string.IsNullOrWhiteSpace(porta)) return false;
        var p = porta.Trim().ToUpperInvariant();
        if (!p.StartsWith("COM")) return false;
        var num = p.Substring(3);
        if (num.Length == 0 || num.Length > 3) return false;
        if (!num.All(char.IsDigit)) return false;
        return int.TryParse(num, out var n) && n >= 1 && n <= 256;
    }

    // Endereço aceito pelo Worker para um T50: IPv4 (T50M/Simulador) ou porta serial (Arduino).
    public static bool EnderecoT50Valido(string? endereco) =>
        IpValido(endereco) || PortaSerialValida(endereco);
}
