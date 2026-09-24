using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class MinhasConsultas : ContentPage
{
    private ObservableCollection<Paciente> pacientes = new();

    public MinhasConsultas()
    {
        InitializeComponent();

        CarregarPacientes();

        PacientesCollectionView.ItemsSource = pacientes;
    }

    // CARREGA OS PACIENTES MOCKADOS
    private void CarregarPacientes()
    {
        pacientes.Add(
            new Paciente
            {
                Nome = "Lucas",
                Consultas = new ObservableCollection<Consulta>
                {
                    new Consulta
                    {
                        Medico = "Dra. Ana Oliveira",
                        Especialidade = "Pediatria",
                        Data = "15/09/2026",
                        Horario = "14:30",
                        Modalidade = "Particular",
                        Valor = "R$ 200,00",
                        Status = "Agendada"
                    },

                    new Consulta
                    {
                        Medico = "Dra. Camila Rodrigues",
                        Especialidade = "Cardiologia",
                        Data = "20/10/2026",
                        Horario = "09:00",
                        Modalidade = "Convênio",
                        Valor = "GEAP",
                        Status = "Agendada"
                    }
                }
            });

        pacientes.Add(
            new Paciente
            {
                Nome = "Matheus",
                Consultas = new ObservableCollection<Consulta>
                {
                    new Consulta
                    {
                        Medico = "Dr. Carlos Mendes",
                        Especialidade = "Pediatria",
                        Data = "22/09/2026",
                        Horario = "10:00",
                        Modalidade = "Particular",
                        Valor = "R$ 200,00",
                        Status = "Agendada"
                    }
                }
            });

        pacientes.Add(
            new Paciente
            {
                Nome = "Sofia",
                Consultas = new ObservableCollection<Consulta>
                {
                    new Consulta
                    {
                        Medico = "Dra. Mariana Costa",
                        Especialidade = "Neuropediatria",
                        Data = "05/10/2026",
                        Horario = "15:30",
                        Modalidade = "Particular",
                        Valor = "R$ 200,00",
                        Status = "Agendada"
                    },

                    new Consulta
                    {
                        Medico = "Dra. Beatriz Lima",
                        Especialidade = "Endocrinologia",
                        Data = "12/11/2026",
                        Horario = "08:30",
                        Modalidade = "Convênio",
                        Valor = "Unimed",
                        Status = "Agendada"
                    }
                }
            });
    }

    // TOQUE NO CARD DO PACIENTE
    private void PacienteCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is BindableObject elemento &&
            elemento.BindingContext is Paciente paciente)
        {
            paciente.IsExpanded = !paciente.IsExpanded;
        }
    }

    // TOQUE NO CARD DA CONSULTA
    private async void ConsultaCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is BindableObject elemento &&
            elemento.BindingContext is Consulta consulta)
        {
            Paciente? paciente = pacientes.FirstOrDefault(
                p => p.Consultas.Contains(consulta));

            if (paciente == null)
                return;

            await Shell.Current.GoToAsync(
                nameof(MinhasConsultasDetalhes),
                new Dictionary<string, object>
                {
                    { "Medico", consulta.Medico },
                    { "Especialidade", consulta.Especialidade },
                    { "Paciente", paciente.Nome },
                    { "Data", consulta.Data },
                    { "Horario", consulta.Horario },
                    { "Modalidade", consulta.Modalidade },
                    { "Valor", consulta.Valor },
                    { "Status", consulta.Status }
                });
        }
    }

    // VOLTAR
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}


// PACIENTE
public class Paciente : INotifyPropertyChanged
{
    private bool isExpanded;

    public string Nome { get; set; } = string.Empty;

    public ObservableCollection<Consulta> Consultas { get; set; } = new();

    public bool IsExpanded
    {
        get => isExpanded;

        set
        {
            if (isExpanded == value)
                return;

            isExpanded = value;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}


// CONSULTA
public class Consulta
{
    public string Medico { get; set; } = string.Empty;

    public string Especialidade { get; set; } = string.Empty;

    public string Data { get; set; } = string.Empty;

    public string Horario { get; set; } = string.Empty;

    public string Modalidade { get; set; } = string.Empty;

    public string Valor { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string DataHorario =>
        $"{Data} • {Horario}";
}