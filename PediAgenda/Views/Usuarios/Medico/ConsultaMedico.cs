namespace PediAgenda.Views.Usuarios.Medico;

public class ConsultaMedico
{
    public Guid Id { get; } = Guid.NewGuid();

    public DateTime Data { get; set; }

    public TimeSpan Horario { get; set; }

    public string Paciente { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string MotivoCancelamento { get; set; } = string.Empty;

    public bool CanceladaPorBloqueio { get; set; }
}