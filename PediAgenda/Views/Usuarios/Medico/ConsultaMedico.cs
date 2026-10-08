using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public class ConsultaMedico
{
    private string status =
        string.Empty;


    public Guid Id
    {
        get;
    } = Guid.NewGuid();


    public int? IdPaciente
    {
        get;
        set;
    }


    public DateTime Data
    {
        get;
        set;
    }


    public TimeSpan Horario
    {
        get;
        set;
    }


    public string Paciente
    {
        get;
        set;
    } = string.Empty;


    public string TipoConsulta
    {
        get;
        set;
    } = "Consulta pediátrica";


    // Qualquer valor colocado aqui é
    // automaticamente padronizado.

    public string Status
    {
        get =>
            status;

        set =>
            status =
                StatusConsulta.Normalizar(
                    value);
    }


    public string MotivoCancelamento
    {
        get;
        set;
    } = string.Empty;


    public bool CanceladaPorBloqueio
    {
        get;
        set;
    }


    public string HorarioTexto =>
        Horario.ToString(
            @"hh\:mm");
}