namespace PediAgenda.Nucleo;

/// Horários de consulta são horários locais da clínica; bloqueios e JWT usam UTC.
public sealed class RelogioClinica : TimeProvider
{
    public static RelogioClinica Instancia { get; } = new();
    public override TimeZoneInfo LocalTimeZone { get; } =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
}
