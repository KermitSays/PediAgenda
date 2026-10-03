using System.Collections.ObjectModel;
using System.ComponentModel;

namespace PediAgenda.Views.Usuarios.Responsavel;

public static class ResponsavelDados
{
    public static ObservableCollection<PacienteResponsavelItem>
        Pacientes
    { get; } =
        new()
        {
            new PacienteResponsavelItem
            {
                Id = 1,

                Nome = "Lucas",

                DataNascimento =
                    new DateTime(
                        2018,
                        4,
                        12),

                Foto =
                    "lucas.png",

                Consultas =
                    new ObservableCollection<ConsultaResponsavelItem>
                    {
                        new ConsultaResponsavelItem
                        {
                            Id = 1,

                            IdMedico = 1,

                            IdHorario = 0,

                            Medico =
                                "Dra. Ana Oliveira",

                            Especialidade =
                                "Pediatria",

                            Data =
                                new DateTime(
                                    2026,
                                    9,
                                    15),

                            Horario =
                                new TimeSpan(
                                    14,
                                    30,
                                    0),

                            Modalidade =
                                "Particular",

                            Valor =
                                "R$ 200,00",

                            Status =
                                "Realizada"
                        },


                        new ConsultaResponsavelItem
                        {
                            Id = 2,

                            IdMedico = 7,

                            IdHorario = 0,

                            Medico =
                                "Dra. Camila Rodrigues",

                            Especialidade =
                                "Cardiologia",

                            Data =
                                new DateTime(
                                    2026,
                                    10,
                                    20),

                            Horario =
                                new TimeSpan(
                                    9,
                                    0,
                                    0),

                            Modalidade =
                                "Convênio",

                            Valor =
                                "GEAP",

                            Status =
                                "Por confirmar"
                        }
                    }
            },


            new PacienteResponsavelItem
            {
                Id = 2,

                Nome = "Matheus",

                DataNascimento =
                    new DateTime(
                        2016,
                        8,
                        25),

                Foto =
                    "matheus.png",

                Consultas =
                    new ObservableCollection<ConsultaResponsavelItem>
                    {
                        new ConsultaResponsavelItem
                        {
                            Id = 3,

                            IdMedico = 2,

                            IdHorario = 0,

                            Medico =
                                "Dr. Carlos Mendes",

                            Especialidade =
                                "Pediatria",

                            Data =
                                new DateTime(
                                    2026,
                                    9,
                                    22),

                            Horario =
                                new TimeSpan(
                                    10,
                                    0,
                                    0),

                            Modalidade =
                                "Particular",

                            Valor =
                                "R$ 200,00",

                            Status =
                                "Realizada"
                        }
                    }
            },


            new PacienteResponsavelItem
            {
                Id = 3,

                Nome = "Sofia",

                DataNascimento =
                    new DateTime(
                        2020,
                        9,
                        27),

                Foto =
                    "sofia.png",

                Consultas =
                    new ObservableCollection<ConsultaResponsavelItem>
                    {
                        new ConsultaResponsavelItem
                        {
                            Id = 4,

                            IdMedico = 4,

                            IdHorario = 0,

                            Medico =
                                "Dra. Mariana Costa",

                            Especialidade =
                                "Neuropediatria",

                            Data =
                                new DateTime(
                                    2026,
                                    10,
                                    5),

                            Horario =
                                new TimeSpan(
                                    15,
                                    30,
                                    0),

                            Modalidade =
                                "Particular",

                            Valor =
                                "R$ 200,00",

                            Status =
                                "Confirmada"
                        },


                        new ConsultaResponsavelItem
                        {
                            Id = 5,

                            IdMedico = 8,

                            IdHorario = 0,

                            Medico =
                                "Dra. Beatriz Lima",

                            Especialidade =
                                "Endocrinologia",

                            Data =
                                new DateTime(
                                    2026,
                                    11,
                                    12),

                            Horario =
                                new TimeSpan(
                                    8,
                                    30,
                                    0),

                            Modalidade =
                                "Convênio",

                            Valor =
                                "Unimed",

                            Status =
                                "Por confirmar"
                        }
                    }
            }
        };
}


public class PacienteResponsavelItem :
    INotifyPropertyChanged
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