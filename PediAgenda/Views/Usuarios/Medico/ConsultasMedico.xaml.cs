using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class ConsultasMedico : ContentPage
{
    public ObservableCollection<ConsultaAgenda> Agenda { get; set; }

    // Construtor da página ConsultasMedico
    public ConsultasMedico()
    {
        InitializeComponent();

        DataConsultaPicker.DateSelected += DataConsultaPicker_DateSelected;
    }

    // Sobrescreve o método OnAppearing para carregar a agenda ao aparecer a página
    protected override void OnAppearing()
    {
        base.OnAppearing();

        CarregarAgenda(
            DataConsultaPicker.Date ?? DateTime.Today);
    }

    // Evento de seleção de data no DatePicker
    private void DataConsultaPicker_DateSelected(
        object? sender,
        DateChangedEventArgs e)
    {
        CarregarAgenda(
            e.NewDate ?? DateTime.Today);
    }

    // Método para carregar a agenda com horários e status
    private void CarregarAgenda(DateTime data)
    {
        Agenda = new ObservableCollection<ConsultaAgenda>();

        for (int hora = 9; hora <= 16; hora++)
        {
            TimeSpan horario = new TimeSpan(hora, 0, 0);

            ConsultaMedico? consulta =
                ConsultasMedicoDados.Consultas
                    .FirstOrDefault(
                        consulta =>
                            consulta.Data.Date == data.Date &&
                            consulta.Horario == horario);

            if (consulta != null)
            {
                Agenda.Add(
                    new ConsultaAgenda
                    {
                        Horario = consulta.Horario.ToString(@"hh\:mm"),
                        Paciente = consulta.Paciente,
                        Status = consulta.Status,
                        StatusCor = ObterCorStatus(consulta.Status)
                    });

                continue;
            }

            AdicionarHorario(
                data,
                horario.ToString(@"hh\:mm"),
                "-----------",
                "Horário disponível",
                Colors.Blue);
        }
    }

    // Método para obter a cor correspondente ao status da consulta
    private Color ObterCorStatus(string status)
    {
        return status switch
        {
            "Confirmado" => Colors.Green,
            "Cancelado" => Colors.Red,
            "Por Confirmar" => Colors.Orange,
            "Horário disponível" => Colors.Blue,
            "Horário bloqueado" => Colors.Red,
            _ => Colors.Black
        };
    }

    // Método para adicionar um horário à agenda
    private void AdicionarHorario(
    DateTime data,
    string horario,
    string paciente,
    string status,
    Color statusCor)
    {
        TimeSpan horarioConsulta =
            TimeSpan.Parse(horario);

        BloqueioHorario? bloqueio =
            EncontrarBloqueio(
                data,
                horarioConsulta);

        if (bloqueio != null)
        {
            Agenda.Add(
                new ConsultaAgenda
                {
                    Horario = horario,
                    Paciente = "-----------",
                    Status = "Horário bloqueado",
                    StatusCor = Colors.Red
                });

            return;
        }

        Agenda.Add(
            new ConsultaAgenda
            {
                Horario = horario,
                Paciente = paciente,
                Status = status,
                StatusCor = statusCor
            });
    }

    // Método para encontrar um bloqueio de horário na agenda
    private BloqueioHorario? EncontrarBloqueio(
        DateTime data,
        TimeSpan horario)
    {
        foreach (BloqueioHorario bloqueio
            in BloqueiosMedico.Bloqueios)
        {
            bool dataDentroDoBloqueio =
                data.Date >= bloqueio.DataInicial.Date &&
                data.Date <= bloqueio.DataFinal.Date;

            if (!dataDentroDoBloqueio)
                continue;

            bool horarioDentroDoBloqueio =
                horario >= bloqueio.HorarioInicial &&
                horario < bloqueio.HorarioFinal;

            if (horarioDentroDoBloqueio)
                return bloqueio;
        }

        return null;
    }

    // Evento de clique do botão "Bloquear Horário" 
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

    // Classe para representar uma consulta na agenda
    public class ConsultaAgenda
    {
        public string Horario { get; set; } = string.Empty;

        public string Paciente { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public Color StatusCor { get; set; } = Colors.Black;
    }

    // Evento de clique do botão "Liberar Horários"
    private async void LiberarHorariosButton_Clicked(
    object sender,
    EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LiberarHorarios));
    }
}