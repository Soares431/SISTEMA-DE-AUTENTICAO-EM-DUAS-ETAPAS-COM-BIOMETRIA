namespace FrontendControleAcesso.Services;

/// <summary>
/// Armazena o token JWT em memória para a sessão Blazor Server.
/// </summary>
public static class TokenStore
{
    public static string? Token { get; set; }
    public static int AdminId { get; set; }
    public static string NomeCompleto { get; set; } = "";

    public static bool EstaAutenticado => !string.IsNullOrEmpty(Token);
}