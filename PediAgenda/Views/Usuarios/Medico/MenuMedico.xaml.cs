using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class MenuMedico : ContentPage
{
    public ObservableCollection<ConsultaHoje> ConsultasLista { get; set; }

    public MenuMedico()
    {
        InitializeComponent();

        // Mostra a data atual
        DataAtualLabel.Text =
            $"Hoje é {DateTime.Today:dd 'de' MMMM 'de' yyyy}";

        // Consultas mockadas
        ConsultasLista = new ObservableCollection<ConsultaHoje>
        {
            new ConsultaHoje
            {
                Horario = "08:00",
                Paciente = "João Silva",
                Status = "Confirmado"
            },

            new ConsultaHoje
            {
                Horario = "09:00",
                Paciente = "Maria Alice",
                Status = "Confirmado"
            },

            new ConsultaHoje
            {
                Horario = "10:00",
                Paciente = "Renata Oliveira",
                Status = "Confirmado"
            },

            new ConsultaHoje
            {
                Horario = "11:00",
                Paciente = "João Pedro",
                Status = "Confirmado"
            },

            new ConsultaHoje
            {
                Horario = "14:00",
                Paciente = "Bianca",
                Status = "Por Confirmar"
            },

            new ConsultaHoje
            {
                Horario = "15:00",
                Paciente = "Thiago",
                Status = "Por Confirmar"
            }
        };

        BindingContext = this;
    }

    // Modelo das consultas de hoje
    public class ConsultaHoje
    {
        public string Horario { get; set; } = string.Empty;

        public string Paciente { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
    }

    // PACIENTES
    private async void MedicoPacientesButton_Clicked(object sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PacientesMedico));
    }

    // CONSULTAS
    private async void MedicoConsultasButton_Clicked(object sender, TappedEventArgs e)
    {
        await DisplayAlertAsync(
            "Consultas",
            "Tela de consultas ainda será implementada.",
            "OK");
    }

    // RELATÓRIOS
    private async void MedicoRelatoriosButton_Clicked(object sender, TappedEventArgs e)
    {
        await DisplayAlertAsync(
            "Relatórios",
            "Tela de Relatórios ainda será implementada.",
            "OK");
    }
}