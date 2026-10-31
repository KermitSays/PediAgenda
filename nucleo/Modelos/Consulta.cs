namespace PediAgenda.Nucleo.Modelos;

/// <summary>
/// Uma consulta já com o que a tela precisa mostrar. A tabela `consulta` só
/// guarda paciente, horário e status (3FN); data, hora e médico vêm do
/// horário reservado.
/// </summary>
public record ConsultaDetalhe(
    int Id, string Status,
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
