namespace PediAgenda.Views.Usuarios.Medico;

public class ConsultaMedico
{
    public Guid Id { get; } =
        Guid.NewGuid();


    // ID DO PACIENTE
    //
    // É nullable porque ainda existem alguns dados temporários
    // que não estão vinculados aos pacientes cadastrados em MedicoDados.
    //
    // Quando a API estiver integrada, este campo será preenchido
    // com o id real vindo do banco.

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


    public string Status
    {
        get;
        set;
    } = string.Empty;


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


    // TEXTO DO HORÁRIO

    public string HorarioTexto =>
        Horario.ToString(
            @"hh\:mm");
}