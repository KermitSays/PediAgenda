namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
public partial class AgendarDataHorario : ContentPage
{
    private int pacienteId;

    private string? paciente;

    private string? medico;

    private string? fotoMedico;

    private string? especialidade;

    private string? horarioSelecionado;

    private bool ajustandoData;


    public int PacienteId
    {
        get => pacienteId;

        set => pacienteId = value;
    }


    public string Paciente
    {
        get => paciente ?? string.Empty;

        set => paciente = value;
    }


    public string Medico
    {
        get => medico ?? string.Empty;

        set
        {
            medico = value;


            if (MedicoLabel != null)
            {
                MedicoLabel.Text =
                    value;
            }
        }
    }


    public string FotoMedico
    {
        get => fotoMedico ?? string.Empty;

        set => fotoMedico = value;
    }


    public string Especialidade
    {
        get => especialidade ?? string.Empty;

        set
        {
            especialidade = value;


            if (EspecialidadeLabel != null)
            {
                EspecialidadeLabel.Text =
                    value;
            }
        }
    }


    public AgendarDataHorario()
    {
        InitializeComponent();


        DataPicker.MinimumDate =
            DateTime.Today;


        DataPicker.Date =
            ProximaDataDisponivel();


        CarregarHorarios();
    }


    // PRÓXIMA DATA DISPONÍVEL

    private DateTime ProximaDataDisponivel()
    {
        DateTime data =
            DateTime.Today;


        while (
            data.DayOfWeek ==
                DayOfWeek.Saturday ||

            data.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            data =
                data.AddDays(1);
        }


        return data;
    }


    // ALTERA A DATA

    private void DataPicker_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        if (ajustandoData)
            return;


        if (e.NewDate.HasValue &&
            (
                e.NewDate.Value.DayOfWeek ==
                    DayOfWeek.Saturday ||

                e.NewDate.Value.DayOfWeek ==
                    DayOfWeek.Sunday
            ))
        {
            ErroHorarioLabel.Text =
                "A clínica não atende aos finais de semana.";


            ErroHorarioLabel.IsVisible =
                true;


            ajustandoData =
                true;


            DataPicker.Date =
                ProximaDataDisponivel();


            ajustandoData =
                false;


            return;
        }


        horarioSelecionado =
            null;


        CarregarHorarios();


        ErroHorarioLabel.IsVisible =
            false;
    }


    // CARREGA OS HORÁRIOS

    private void CarregarHorarios()
    {
        HorariosContainer.Children.Clear();


        string[] horarios =
        {
            "08:00",
            "08:30",
            "09:00",
            "09:30",
            "10:00",
            "10:30",
            "13:00",
            "13:30",
            "14:00",
            "14:30",
            "15:00",
            "15:30",
            "16:00",
            "16:30"
        };


        foreach (string horario in horarios)
        {
            HorariosContainer.Children.Add(
                CriarCardHorario(horario));
        }
    }


    // CRIA O CARD DO HORÁRIO

    private Border CriarCardHorario(
        string horario)
    {
        Border card =
            new Border
            {
                BackgroundColor =
                    (Color)Application.Current.Resources["White"],

                Stroke =
                    (Color)Application.Current.Resources["InputBlue"],

                StrokeThickness =
                    1,

                Padding =
                    new Thickness(18, 12),

                Margin =
                    new Thickness(0, 0, 10, 10),

                StrokeShape =
                    new Microsoft.Maui.Controls.Shapes.RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(12)
                    }
            };


        Label horarioLabel =
            new Label
            {
                Text =
                    horario,

                FontSize =
                    16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    (Color)Application.Current.Resources["TextDark"],

                HorizontalOptions =
                    LayoutOptions.Center,

                VerticalOptions =
                    LayoutOptions.Center
            };


        card.Content =
            horarioLabel;


        TapGestureRecognizer tap =
            new TapGestureRecognizer();


        tap.Tapped +=
            (sender, e) =>
            {
                horarioSelecionado =
                    horario;


                foreach (View item
                    in HorariosContainer.Children)
                {
                    if (item is Border outroCard)
                    {
                        outroCard.BackgroundColor =
                            (Color)Application.Current.Resources["White"];

                        outroCard.Stroke =
                            (Color)Application.Current.Resources["InputBlue"];

                        outroCard.StrokeThickness =
                            1;
                    }
                }


                card.BackgroundColor =
                    Color.FromArgb("#EAF5FA");


                card.Stroke =
                    (Color)Application.Current.Resources["PrimaryBlue"];


                card.StrokeThickness =
                    2.5;


                ErroHorarioLabel.IsVisible =
                    false;
            };


        card.GestureRecognizers.Add(
            tap);


        return card;
    }


    // CONTINUAR

    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrEmpty(
            horarioSelecionado))
        {
            ErroHorarioLabel.Text =
                "Selecione um horário para continuar.";


            ErroHorarioLabel.IsVisible =
                true;


            return;
        }


        DateTime dataEscolhida =
            DataPicker.Date
            ?? DateTime.Today;


        string dataSelecionada =
            dataEscolhida.ToString(
                "dd/MM/yyyy");


        await Shell.Current.GoToAsync(
            nameof(AgendarTipoAtendimento),
            new Dictionary<string, object>
            {
                {
                    "PacienteId",
                    PacienteId
                },

                {
                    "Paciente",
                    Paciente
                },

                {
                    "Medico",
                    Medico
                },

                {
                    "FotoMedico",
                    FotoMedico
                },

                {
                    "Especialidade",
                    Especialidade
                },

                {
                    "Data",
                    dataSelecionada
                },

                {
                    "Horario",
                    horarioSelecionado
                }
            });
    }


    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}