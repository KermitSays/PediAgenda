using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PediAgenda.Views.Usuarios.Responsavel;

public static class ResponsavelDados
{
    // A lista começa vazia, sem pacientes de exemplo.
    public static ObservableCollection<PacienteResponsavelItem>
        Pacientes
    { get; } = new();
}


public class PacienteResponsavelItem: INotifyPropertyChanged
{
    private bool isExpanded;
    private bool isHistoricoExpanded;


    public int Id { get; set; }


    public string Nome { get; set; } =
        string.Empty;


    public DateTime DataNascimento
    {
        get;
        set;
    }


    public string Foto { get; set; } =
        string.Empty;


    public ObservableCollection<ConsultaResponsavelItem>
        Consultas
    { get; set; } =
            new();


    public int Idade
    {
        get
        {
            DateTime hoje =
                DateTime.Today;


            int idade =
                hoje.Year -
                DataNascimento.Year;


            if (DataNascimento.Date >
                hoje.AddYears(
                    -idade))
            {
                idade--;
            }


            return idade;
        }
    }


    // CONSULTAS ATIVAS
    //
    // Mais próxima primeiro.

    public IEnumerable<ConsultaResponsavelItem>
        ConsultasAtivasOrdenadas =>
            Consultas
                .Where(c =>
                    c.Status.Equals(
                        "Confirmada",
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    c.Status.Equals(
                        "Por confirmar",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(c =>
                    c.Data)
                .ThenBy(c =>
                    c.Horario);


    public bool TemConsultasAtivas =>
        ConsultasAtivasOrdenadas.Any();


    public bool SemConsultasAtivas =>
        !TemConsultasAtivas;


    // HISTÓRICO
    //
    // Somente consultas REALIZADAS.
    //
    // Mais recente primeiro.

    public IEnumerable<ConsultaResponsavelItem>
        HistoricoConsultasOrdenado =>
            Consultas
                .Where(c =>
                    c.Status.Equals(
                        "Realizada",
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(c =>
                    c.Data)
                .ThenByDescending(c =>
                    c.Horario);


    public bool TemHistorico =>
        HistoricoConsultasOrdenado.Any();


    public bool SemHistorico =>
        !TemHistorico;


    public bool IsExpanded
    {
        get => isExpanded;

        set
        {
            if (isExpanded == value)
                return;


            isExpanded =
                value;


            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    nameof(IsExpanded)));
        }
    }


    public bool IsHistoricoExpanded
    {
        get => isHistoricoExpanded;

        set
        {
            if (isHistoricoExpanded == value)
                return;


            isHistoricoExpanded =
                value;


            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(
                    nameof(IsHistoricoExpanded)));
        }
    }


    public event PropertyChangedEventHandler?
        PropertyChanged;
}


public class ConsultaResponsavelItem
{
    public int Id { get; set; }


    // Correspondem futuramente às FKs
    // utilizadas pelo banco/API.

    public int IdMedico { get; set; }


    public int IdHorario { get; set; }


    public string Medico { get; set; } =
        string.Empty;


    public string Especialidade { get; set; } =
        string.Empty;


    public DateTime Data { get; set; }


    public TimeSpan Horario { get; set; }


    public string Modalidade { get; set; } =
        string.Empty;


    public string Valor { get; set; } =
        string.Empty;


    public string Status { get; set; } =
        string.Empty;


    public string DataFormatada =>
        Data.ToString(
            "dd/MM/yyyy");


    public string HorarioFormatado =>
        Horario.ToString(
            @"hh\:mm");


    public string DataHorario =>
        $"{Data:dd/MM/yyyy} • {HorarioFormatado}";
}