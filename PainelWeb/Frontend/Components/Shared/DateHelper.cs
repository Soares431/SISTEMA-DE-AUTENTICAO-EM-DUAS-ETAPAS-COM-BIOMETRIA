namespace FrontendControleAcesso.Components.Shared;

public static class DateHelper
{

    public static DateTime Local(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

    public static DateTime? Local(DateTime? utc) =>
        utc.HasValue ? Local(utc.Value) : null;
}

