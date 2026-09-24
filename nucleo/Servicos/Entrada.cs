using System.Globalization;

namespace PediAgenda.Nucleo.Servicos;

/// <summary>
/// Leitura das datas e horas que chegam como texto nas chamadas. Um erro vira
/// uma mensagem no campo correspondente, para a tela destacar.
/// </summary>
public static class Entrada
{
    public static DateOnly? LerData(string? valor, string campo, Dictionary<string, string> campos)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            campos[campo] = "Informe a data.";
            return null;
        }
        if (DateOnly.TryParseExact(valor.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var data))
            return data;

        campos[campo] = "Data inválida. Use o formato AAAA-MM-DD.";
        return null;
    }

    public static TimeOnly? LerHora(string? valor, string campo, Dictionary<string, string> campos)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            campos[campo] = "Informe a hora.";
            return null;
        }
        if (TimeOnly.TryParseExact(valor.Trim(), "HH:mm", CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var hora))
            return hora;

        campos[campo] = "Hora inválida. Use o formato HH:MM.";
        return null;
    }
}
