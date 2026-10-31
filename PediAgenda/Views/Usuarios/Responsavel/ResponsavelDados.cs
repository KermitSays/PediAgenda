using System.Collections.ObjectModel;
using System.ComponentModel;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Responsavel;

public static class ResponsavelDados
{
    public static ObservableCollection<PacienteResponsavelItem>
        Pacientes
    {
        get;
    } = new();
}


public class PacienteResponsavelItem :
    INotifyPropertyChanged
{
    private bool isExpanded;

    private bool isHistoricoExpanded;


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


    public string Foto
    {
        get;
        set;
    } = string.Empty;


    public ObservableCollection<ConsultaResponsavelItem>
        Consultas
    {
        get;
        set;
    } = new();


    public int Idade
    {
        get
        {
            DateTime hoje =
                DateTime.Today;


            int idade =
                hoje.Year -
                DataNascimento.Year;


            if (
                DataNascimento.Date >
                hoje.AddYears(
                    -idade))
            {
                idade--;
            }


            return idade;
        }
    }

    // Filtro selecionado no histórico de consultas
    private string filtroSelecionado = "Todas";

    public string FiltroSelecionado
    {
        get => filtroSelecionado;
        set
        {
            if (filtroSelecionado == value)
                return;

            filtroSelecionado = value;

            PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(nameof(FiltroSelecionado)));

            PropertyChanged?.Invoke(
                this, new PropertyChangedEventArgs(nameof(TodasConsultasOrdenadas)));
        }
    }

    // Consultas exibidas conforme o filtro selecionado
    public IEnumerable<ConsultaResponsavelItem> TodasConsultasOrdenadas
    {
        get
        {
            IEnumerable<ConsultaResponsavelItem> consultasFiltradas = Consultas;

            switch (FiltroSelecionado)
            {
                case "Agendadas":
                    consultasFiltradas = Consultas.Where(c =>
                        StatusConsulta.EhPorConfirmar(c.Status) ||
                        StatusConsulta.EhConfirmada(c.Status));
                    break;

                case "Realizadas":
                    consultasFiltradas = Consultas.Where(c =>
                        StatusConsulta.EhRealizada(c.Status));
                    break;

                case "Canceladas":
                    consultasFiltradas = Consultas.Where(c =>
                        StatusConsulta.EhCancelada(c.Status));
                    break;
            }

            return consultasFiltradas
                .OrderByDescending(c => c.Data)
                .ThenByDescending(c => c.Horario);
        }
    }

    public bool SemConsultas =>
        !Consultas.Any();

    // =============================================
    // CONSULTAS ATIVAS
    // =============================================

    public IEnumerable<ConsultaResponsavelItem>
        ConsultasAtivasOrdenadas =>
            Consultas
                .Where(c =>
                    StatusConsulta
                        .EhConfirmada(
                            c.Status)

                    ||

                    StatusConsulta
                        .EhPorConfirmar(
                            c.Status))
                .OrderBy(c =>
                    c.Data)
                .ThenBy(c =>
                    c.Horario);


    public bool TemConsultasAtivas =>
        ConsultasAtivasOrdenadas.Any();


    public bool SemConsultasAtivas =>
        !TemConsultasAtivas;


    // =============================================
    // HISTÓRICO
    // =============================================

    public IEnumerable<ConsultaResponsavelItem>
        HistoricoConsultasOrdenado =>
            Consultas
                .Where(c =>
                    StatusConsulta
                        .EhRealizada(
                            c.Status))
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
        get =>
            isExpanded;

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
        get =>
            isHistoricoExpanded;

        set
        {
            if (
                isHistoricoExpanded ==
                value)
            {
                return;
            }


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
    private string status =
        string.Empty;


    public int Id
    {
        get;
        set;
    }


    public int IdMedico
    {
        get;
        set;
    }


    public int IdHorario
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


    public string Valor
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

    // Status apresentado na tela do Responsável.
    // Não altera o status utilizado nas regras do sistema.
    public string StatusExibicao =>
        StatusConsulta.EhPorConfirmar(Status)
            ? "Agendada"
            : Status;


    public string DataFormatada =>
        Data.ToString(
            "dd/MM/yyyy");


    public string HorarioFormatado =>
        Horario.ToString(
            @"hh\:mm");


    public string DataHorario =>
        $"{Data:dd/MM/yyyy} • {HorarioFormatado}";
}