namespace PediAgenda.Views.Usuarios.Medico;

public class BloqueioHorario
{
    public Guid Id
    {
        get;
    } =
        Guid.NewGuid();


    // Enquanto os bloqueios antigos eram sempre
    // da Dra. Ana, usamos 1 como padrão.
    //
    // Quando a API assumir a agenda, este valor
    // virá do médico selecionado/autenticado.

    public int IdMedico
    {
        get;
        set;
    } = 1;


    public DateTime DataInicial
    {
        get;
        set;
    }


    public DateTime DataFinal
    {
        get;
        set;
    }


    public TimeSpan HorarioInicial
    {
        get;
        set;
    }


    public TimeSpan HorarioFinal
    {
        get;
        set;
    }


    public string Motivo
    {
        get;
        set;
    } = string.Empty;
}