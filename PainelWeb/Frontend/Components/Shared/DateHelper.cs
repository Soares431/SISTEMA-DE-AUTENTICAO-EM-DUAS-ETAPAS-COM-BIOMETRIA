namespace FrontendControleAcesso.Components.Shared;

/// <summary>
/// Utilitário de conversão de datas para exibição no painel.
/// SQLite + EF Core retorna DateTime com Kind=Unspecified, mas os valores são UTC
/// (gravados via DateTime.UtcNow). Este helper converte para o horário local do servidor.
/// </summary>
public static class DateHelper
{
    /// <summary>
    /// Converte um DateTime armazenado como UTC (Kind=Unspecified pelo SQLite)
    /// para o horário local do servidor antes de exibir ao usuário.
    /// </summary>
    public static DateTime Local(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

    /// <summary>
    /// Versão nullable — retorna null se a entrada for null.
    /// </summary>
    public static DateTime? Local(DateTime? utc) =>
        utc.HasValue ? Local(utc.Value) : null;
}
