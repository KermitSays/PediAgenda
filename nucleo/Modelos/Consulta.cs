namespace PediAgenda.Nucleo.Modelos;

/// <summary>
/// Uma consulta já com o que a tela precisa mostrar. A tabela `consulta` só
/// guarda paciente, horário, status e tipo (3FN); data, hora e médico vêm do
/// horário reservado.
/// </summary>
public record ConsultaDetalhe(
    int Id, string Status, string TipoAtendimento,
    DateOnly Data, TimeOnly Inicio, TimeOnly Fim,
    int IdMedico, string NomeMedico, string Especialidade,
    int IdPaciente, string NomePaciente);

public static class StatusConsulta
{
    public const string Agendada = "AGENDADA";
    public const string Confirmada = "CONFIRMADA";
    public const string Cancelada = "CANCELADA";
    public const string Realizada = "REALIZADA";
}

public static class TipoAtendimento
{
    public const string Convenio = "CONVENIO";
    public const string Particular = "PARTICULAR";

    public static readonly IReadOnlySet<string> Validos = new HashSet<string> { Convenio, Particular };
}
