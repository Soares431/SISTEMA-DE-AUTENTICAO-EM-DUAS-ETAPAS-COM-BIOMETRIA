namespace FrontendControleAcesso.Services;

/// <summary>
/// Armazena o token JWT em memória para a sessão Blazor Server.
/// </summary>
public interface ITokenStore
{
    string? Token { get; set; }
    int AdminId { get; set; }
    string NomeCompleto { get; set; }
    bool EstaAutenticado { get; }
}

// Registrada como Scoped — uma instância por circuito Blazor (por usuário conectado).
// Era static antes (bug #1): estado compartilhado entre todos os admins logados simultaneamente.
public class TokenStore : ITokenStore
{
    public string? Token { get; set; }
    public int AdminId { get; set; }
    public string NomeCompleto { get; set; } = "";

    public bool EstaAutenticado => !string.IsNullOrEmpty(Token);
}
