namespace PediAgenda.Nucleo.Modelos;

/// <summary>
/// Um bloco da agenda que pode ser agendado agora, já com o médico dono dele.
/// Não é uma tabela: é o resultado de horario + agenda_medica + medico + usuario.
/// </summary>
public record HorarioLivre(int IdHorario, DateOnly Data, TimeOnly Inicio, TimeOnly Fim,
                           int IdMedico, string NomeMedico, string Especialidade);
