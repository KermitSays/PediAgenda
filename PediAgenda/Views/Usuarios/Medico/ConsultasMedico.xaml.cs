using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class ConsultasMedico : ContentPage
{
    public ObservableCollection<ConsultaAgenda> Agenda { get; set; }

    public ConsultasMedico()
    {
        InitializeComponent();

        DataConsultaPicker.DateSelected += DataConsultaPicker_DateSelected;

        CarregarAgenda(DataConsultaPicker.Date ?? DateTime.Today);
    }

    private void DataConsultaPicker_DateSelected(
        object? sender,
        DateChangedEventArgs e)
    {
        CarregarAgenda(e.NewDate ?? DateTime.Today);
    }

    private void CarregarAgenda(DateTime data)
    {
        // Agenda mockada da data selecionada
        Agenda = new ObservableCollection<ConsultaAgenda>
        {
            new ConsultaAgenda
            {
                Horario = "09:00",
                Paciente = "Maria Alice",
                Status = "Confirmado",
                StatusCor = Colors.Green
            },

            new ConsultaAgenda
            {
                Horario = "10:00",
                Paciente = "João Pedro",
                Status = "Confirmado",
                StatusCor = Colors.Green
            },

            new ConsultaAgenda
            {
                Horario = "11:00",
                Paciente = "Renata Oliveira",
                Status = "Confirmado",
                StatusCor = Colors.Green
            },

            new ConsultaAgenda
            {
                Horario = "12:00",
                Paciente = "Isaac",
                Status = "Cancelado",
                StatusCor = Colors.Red
            },

            new ConsultaAgenda
            {
                Horario = "13:00",
                Paciente = "Thiago",
                Status = "Por Confirmar",
                StatusCor = Colors.Orange
            },

            new ConsultaAgenda
            {
                Horario = "14:00",
                Paciente = "Bianca",
                Status = "Por Confirmar",
                StatusCor = Colors.Orange
            },

            new ConsultaAgenda
            {
                Horario = "15:00",
                Paciente = "-----------",
                Status = "Horário disponível",
                StatusCor = Colors.Blue
            },

            new ConsultaAgenda
            {
                Horario = "16:00",
                Paciente = "-----------",
                Status = "Horário disponível",
                StatusCor = Colors.Blue
            }
        };

        AgendaCollectionView.ItemsSource = Agenda;
    }

    private async void BloquearHorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(BloquearHorario));
    }

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    public class ConsultaAgenda
    {
        public string Horario { get; set; } = string.Empty;

        public string Paciente { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public Color StatusCor { get; set; } = Colors.Black;
    }
}