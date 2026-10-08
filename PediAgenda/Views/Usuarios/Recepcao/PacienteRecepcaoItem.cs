using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Recepcao;

public class PacienteRecepcaoItem
{
    public int Id
    {
        get;
        set;
    }


    public string Nome
    {
        get;
        set;
    } = string.Empty;


    public DateTime DataNascimento
    {
        get;
        set;
    }


    public string Responsavel
    {
        get;
        set;
    } = string.Empty;


    public string TelefoneResponsavel
    {
        get;
        set;
    } = string.Empty;


    public string EmailResponsavel
    {
        get;
        set;
    } = string.Empty;


    public string Status
    {
        get;
        set;
    } = string.Empty;


    public List<ConsultaPacienteRecepcaoItem>
        Consultas
    {
        get;
        set;
    } = new();


    public string ResponsavelFormatado =>
        $"Responsável: {Responsavel}";


    public string NascimentoFormatado =>
        $"Nascimento: {DataNascimento:dd/MM/yyyy}";
}


public class ConsultaPacienteRecepcaoItem
{
    private string status =
        string.Empty;


    public int? HorarioId
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


    public string Medico
    {
        get;
        set;
    } = string.Empty;


    public string Especialidade
    {
        get;
        set;
    } = string.Empty;


    public string TipoAtendimento
    {
        get;
        set;
    } = string.Empty;


    public string Status
    {
        get =>
            status;

        set =>
            status =
                StatusConsulta.Normalizar(
                    value);
    }


    public string DataHoraFormatada =>
        $"{Data:dd/MM/yyyy} • {Horario:hh\\:mm}";


    public string TextoAcesso =>
        HorarioId.HasValue
            ? "Toque para acessar"
            : "Consulta do histórico";
}