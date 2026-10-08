using System.Collections.ObjectModel;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class ConsultasHojeRecepcao : ContentPage
{
    public ObservableCollection<ConsultaRecepcaoResumo>
        ConsultasHoje
    { get; } = new();

    public ConsultasHojeRecepcao()
    {
        InitializeComponent();

        // Exemplos para conferir o layout.
        // Depois serão substituídos pelas consultas do banco.
        ConsultasHoje.Add(new ConsultaRecepcaoResumo
        {
            Horario = "08:00",
            Paciente = "Maria Alice",
            Medico = "Dr. Carlos Mendes",
            Status = "Confirmado"
        });

        ConsultasHoje.Add(new ConsultaRecepcaoResumo
        {
            Horario = "09:00",
            Paciente = "João Pedro",
            Medico = "Dra. Fernanda Lima",
            Status = "Por confirmar"
        });

        ConsultasHoje.Add(new ConsultaRecepcaoResumo
        {
            Horario = "10:00",
            Paciente = "Bianca",
            Medico = "Dr. Carlos Mendes",
            Status = "Confirmado"
        });

        ConsultasHoje.Add(new ConsultaRecepcaoResumo
        {
            Horario = "11:00",
            Paciente = "Isaac",
            Medico = "Dra. Fernanda Lima",
            Status = "Cancelado"
        });

        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ResumoLabel.Text =
            $"{DateTime.Today:dd/MM/yyyy} • " +
            $"{ConsultasHoje.Count} consulta(s) — dados de exemplo";
    }

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

public class ConsultaRecepcaoResumo
{
    public string Horario { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;
    private string status =
    string.Empty;


    public string Status
    {
        get =>
            status;

        set =>
            status =
                StatusConsulta.Normalizar(
                    value);
    }
}