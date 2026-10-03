namespace PediAgenda.Views.Usuarios.Recepcao;

public class HorarioAgendaRecepcao
{
    public int Id { get; set; }

    public string Medico { get; set; } = string.Empty;

    public DateTime Data { get; set; }

    public TimeSpan Horario { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Paciente { get; set; } = string.Empty;

    public string MotivoBloqueio { get; set; } = string.Empty;


    public string HorarioFormatado =>
        Horario.ToString(@"hh\:mm");


    public string Descricao
    {
        get
        {
            if (Status == "Agendado")
                return Paciente;

            if (Status == "Bloqueado")
                return $"Motivo: {MotivoBloqueio}";

            return "Horário disponível";
        }
    }


    public string TextoAcao
    {
        get
        {
            return Status switch
            {
                "Disponível" => "AGENDAR",
                "Agendado" => "VER CONSULTA",
                "Bloqueado" => "LIBERAR",
                _ => "ACESSAR"
            };
        }
    }
}