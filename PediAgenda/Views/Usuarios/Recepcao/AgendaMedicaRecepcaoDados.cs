namespace PediAgenda.Views.Usuarios.Recepcao;

public static class AgendaMedicaRecepcaoDados
{
    public static List<string> Medicos { get; } = new()
    {
        "Dr. Carlos Mendes",
        "Dra. Fernanda Lima"
    };


    public static List<HorarioAgendaRecepcao> Horarios { get; } = new()
    {
        new HorarioAgendaRecepcao
        {
            Id = 1,
            Medico = "Dr. Carlos Mendes",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(8, 0, 0),
            Status = "Disponível"
        },

        new HorarioAgendaRecepcao
        {
            Id = 2,
            Medico = "Dr. Carlos Mendes",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(9, 0, 0),
            Status = "Agendado",
            Paciente = "Maria Alice"
        },

        new HorarioAgendaRecepcao
        {
            Id = 3,
            Medico = "Dr. Carlos Mendes",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(10, 0, 0),
            Status = "Bloqueado",
            MotivoBloqueio = "Reunião da equipe"
        },

        new HorarioAgendaRecepcao
        {
            Id = 4,
            Medico = "Dr. Carlos Mendes",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(11, 0, 0),
            Status = "Disponível"
        },

        new HorarioAgendaRecepcao
        {
            Id = 5,
            Medico = "Dr. Carlos Mendes",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(14, 0, 0),
            Status = "Agendado",
            Paciente = "Bianca"
        },

        new HorarioAgendaRecepcao
        {
            Id = 6,
            Medico = "Dra. Fernanda Lima",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(8, 0, 0),
            Status = "Agendado",
            Paciente = "João Pedro"
        },

        new HorarioAgendaRecepcao
        {
            Id = 7,
            Medico = "Dra. Fernanda Lima",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(9, 0, 0),
            Status = "Disponível"
        },

        new HorarioAgendaRecepcao
        {
            Id = 8,
            Medico = "Dra. Fernanda Lima",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(10, 0, 0),
            Status = "Bloqueado",
            MotivoBloqueio = "Atendimento externo"
        },

        new HorarioAgendaRecepcao
        {
            Id = 9,
            Medico = "Dra. Fernanda Lima",
            Data = new DateTime(2026, 10, 5),
            Horario = new TimeSpan(11, 0, 0),
            Status = "Disponível"
        }
    };
}