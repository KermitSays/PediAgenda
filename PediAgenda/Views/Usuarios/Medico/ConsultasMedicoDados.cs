namespace PediAgenda.Views.Usuarios.Medico;

public static class ConsultasMedicoDados
{
    public static List<ConsultaMedico> Consultas { get; } = new()
    {
        new ConsultaMedico
        {
            Data = new DateTime(2026, 9, 30),
            Horario = new TimeSpan(9, 0, 0),
            Paciente = "Maria Alice",
            Status = "Confirmado"
        },

        new ConsultaMedico
        {
            Data = new DateTime(2026, 9, 30),
            Horario = new TimeSpan(10, 0, 0),
            Paciente = "João Pedro",
            Status = "Confirmado"
        },

        new ConsultaMedico
        {
            Data = new DateTime(2026, 9, 30),
            Horario = new TimeSpan(11, 0, 0),
            Paciente = "Renata Oliveira",
            Status = "Confirmado"
        },

        new ConsultaMedico
        {
            Data = new DateTime(2026, 9, 30),
            Horario = new TimeSpan(12, 0, 0),
            Paciente = "Isaac",
            Status = "Cancelado"
        },

        new ConsultaMedico
        {
            Data = new DateTime(2026, 9, 30),
            Horario = new TimeSpan(13, 0, 0),
            Paciente = "Thiago",
            Status = "Por Confirmar"
        },

        new ConsultaMedico
        {
            Data = new DateTime(2026, 9, 30),
            Horario = new TimeSpan(14, 0, 0),
            Paciente = "Bianca",
            Status = "Por Confirmar"
        },

        new ConsultaMedico
        {
            Data = new DateTime(2026, 10, 2),
            Horario = new TimeSpan(14, 0, 0),
            Paciente = "Bianca",
            Status = "Por Confirmar"
        },

        new ConsultaMedico
        {
            Data = new DateTime(2026, 10, 2),
            Horario = new TimeSpan(15, 0, 0),
            Paciente = "Thiago",
            Status = "Por Confirmar"
        }
    };
}