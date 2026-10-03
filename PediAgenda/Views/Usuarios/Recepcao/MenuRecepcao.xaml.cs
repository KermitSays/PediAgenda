using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class MenuRecepcao : ContentPage
{
    public ObservableCollection<ConsultaRecepcaoResumo> ConsultasHoje { get; set; }


    public MenuRecepcao()
    {
        InitializeComponent();

        DataAtualLabel.Text =
            $"Hoje é {DateTime.Today:dd 'de' MMMM 'de' yyyy}";


        ConsultasHoje = new ObservableCollection<ConsultaRecepcaoResumo>
        {
            new ConsultaRecepcaoResumo
            {
                Horario = "08:00",
                Paciente = "Maria Alice",
                Medico = "Dr. Carlos Mendes",
                Status = "Confirmado"
            },

            new ConsultaRecepcaoResumo
            {
                Horario = "09:00",
                Paciente = "João Pedro",
                Medico = "Dra. Fernanda Lima",
                Status = "Por Confirmar"
            },

            new ConsultaRecepcaoResumo
            {
                Horario = "10:00",
                Paciente = "Bianca",
                Medico = "Dr. Carlos Mendes",
                Status = "Confirmado"
            },

            new ConsultaRecepcaoResumo
            {
                Horario = "11:00",
                Paciente = "Isaac",
                Medico = "Dra. Fernanda Lima",
                Status = "Cancelado"
            }
        };


        QuantidadeConsultasLabel.Text =
            $"{ConsultasHoje.Count} consulta(s)";


        BindingContext = this;
    }


    // PACIENTES
    private async void PacientesButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(PacientesRecepcao));
    }


    // MÉDICOS
    private async void MedicosButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MedicosRecepcao));
    }
}


// Modelo usado apenas no resumo do menu
public class ConsultaRecepcaoResumo
{
    public string Horario { get; set; } = string.Empty;

    public string Paciente { get; set; } = string.Empty;

    public string Medico { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}