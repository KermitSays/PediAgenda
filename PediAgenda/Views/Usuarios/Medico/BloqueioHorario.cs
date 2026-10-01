namespace PediAgenda.Views.Usuarios.Medico;

public class BloqueioHorario
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime DataInicial { get; set; }

    public DateTime DataFinal { get; set; }

    public TimeSpan HorarioInicial { get; set; }

    public TimeSpan HorarioFinal { get; set; }

    public string Motivo { get; set; } = string.Empty;
}