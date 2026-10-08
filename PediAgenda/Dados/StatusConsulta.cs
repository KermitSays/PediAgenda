namespace PediAgenda.Dados;

public static class StatusConsulta
{
    public const string PorConfirmar =
        "Por confirmar";

    public const string Confirmada =
        "Confirmada";

    public const string Cancelada =
        "Cancelada";

    public const string Realizada =
        "Realizada";


    // =============================================
    // NORMALIZAÇÃO
    // =============================================

    public static string Normalizar(
        string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return string.Empty;


        string valor =
            status.Trim();


        if (
            valor.Equals(
                "Agendada",
                StringComparison.OrdinalIgnoreCase)

            ||

            valor.Equals(
                "Agendado",
                StringComparison.OrdinalIgnoreCase)

            ||

            valor.Equals(
                "Por Confirmar",
                StringComparison.OrdinalIgnoreCase)

            ||

            valor.Equals(
                "Por confirmar",
                StringComparison.OrdinalIgnoreCase))
        {
            return PorConfirmar;
        }


        if (
            valor.Equals(
                "Confirmado",
                StringComparison.OrdinalIgnoreCase)

            ||

            valor.Equals(
                "Confirmada",
                StringComparison.OrdinalIgnoreCase))
        {
            return Confirmada;
        }


        if (
            valor.Equals(
                "Cancelado",
                StringComparison.OrdinalIgnoreCase)

            ||

            valor.Equals(
                "Cancelada",
                StringComparison.OrdinalIgnoreCase))
        {
            return Cancelada;
        }


        if (
            valor.Equals(
                "Realizado",
                StringComparison.OrdinalIgnoreCase)

            ||

            valor.Equals(
                "Realizada",
                StringComparison.OrdinalIgnoreCase))
        {
            return Realizada;
        }


        return valor;
    }


    // =============================================
    // VERIFICAÇÕES
    // =============================================

    public static bool EhPorConfirmar(
        string? status)
    {
        return Normalizar(status) ==
            PorConfirmar;
    }


    public static bool EhConfirmada(
        string? status)
    {
        return Normalizar(status) ==
            Confirmada;
    }


    public static bool EhCancelada(
        string? status)
    {
        return Normalizar(status) ==
            Cancelada;
    }


    public static bool EhRealizada(
        string? status)
    {
        return Normalizar(status) ==
            Realizada;
    }


    public static bool EhAtiva(
        string? status)
    {
        return
            !EhCancelada(status)

            &&

            !EhRealizada(status);
    }
}