namespace FrontendControleAcesso.Services;

public interface ITokenStore
{
    string? Token { get; set; }
    int AdminId { get; set; }
    string NomeCompleto { get; set; }
    bool EstaAutenticado { get; }
}

public class TokenStore : ITokenStore
{
    public string? Token { get; set; }
    public int AdminId { get; set; }
    public string NomeCompleto { get; set; } = "";

    public bool EstaAutenticado => !string.IsNullOrEmpty(Token);
}

