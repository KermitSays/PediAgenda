namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(MedicoId), "MedicoId")]
[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
public partial class AgendarDataHorario : ContentPage
{
    private int pacienteId;

    private int medicoId;

    private string? paciente;

    private string? medico;

    private string? fotoMedico;

    private string? especialidade;

    private HorarioAgendamentoItem?
        horarioSelecionado;

    private bool ajustandoData;


    public int PacienteId
    {
        get => pacienteId;

        set => pacienteId = value;
    }


    public int MedicoId
    {
        get => medicoId;

        set
        {
            medicoId = value;

            if (HorariosContainer != null)
            {
                CarregarHorarios();
            }
        }
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
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


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


            DateTime proximaData =
                e.NewDate.Value.AddDays(1);


            while (
                proximaData.DayOfWeek ==
                    DayOfWeek.Saturday ||

                proximaData.DayOfWeek ==
                    DayOfWeek.Sunday)
            {
                proximaData =
                    proximaData.AddDays(1);
            }


            DataPicker.Date =
                proximaData;


            ajustandoData =
                false;


            horarioSelecionado =
                null;


            CarregarHorarios();


            return;
        }


        horarioSelecionado =
            null;


        CarregarHorarios();


        ErroHorarioLabel.IsVisible =
            false;
    }


    // CARREGA OS HORÁRIOS DISPONÍVEIS

    private void CarregarHorarios()
    {
        if (HorariosContainer == null ||
            DataPicker == null ||
            MedicoId <= 0)
        {
            return;
        }


        HorariosContainer.Children.Clear();


        horarioSelecionado =
            null;


        DateTime dataSelecionada =
            DataPicker.Date
            ?? DateTime.Today;


        List<HorarioAgendamentoItem>
            horariosDisponiveis =
                HorarioAgendamentoDados
                    .ObterHorariosDisponiveis(
                        MedicoId,
                        dataSelecionada);


        if (horariosDisponiveis.Count == 0)
        {
            ErroHorarioLabel.Text =
                "Não existem horários disponíveis para esta data.";


            ErroHorarioLabel.IsVisible =
                true;


            return;
        }


        ErroHorarioLabel.IsVisible =
            false;


        foreach (
            HorarioAgendamentoItem horario
            in horariosDisponiveis)
        {
            HorariosContainer.Children.Add(
                CriarCardHorario(
                    horario));
        }
    }


    // CRIA O CARD DO HORÁRIO

    private Border CriarCardHorario(
        HorarioAgendamentoItem horario)
    {
        Border card =
            new Border
            {
                BackgroundColor =
                    (Color)Application.Current!
                        .Resources["White"],

                Stroke =
                    (Color)Application.Current!
                        .Resources["InputBlue"],

                StrokeThickness =
                    1,

                Padding =
                    new Thickness(
                        18,
                        12),

                Margin =
                    new Thickness(
                        0,
                        0,
                        10,
                        10),

                StrokeShape =
                    new Microsoft.Maui.Controls
                        .Shapes.RoundRectangle
                    {
                        CornerRadius =
                            new CornerRadius(12)
                    }
            };


        Label horarioLabel =
            new Label
            {
                Text =
                    horario.HoraTexto,

                FontSize =
                    16,

                FontAttributes =
                    FontAttributes.Bold,

                TextColor =
                    (Color)Application.Current!
                        .Resources["TextDark"],

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


                foreach (
                    View item
                    in HorariosContainer.Children)
                {
                    if (item
                        is Border outroCard)
                    {
                        outroCard.BackgroundColor =
                            (Color)Application.Current!
                                .Resources["White"];

                        outroCard.Stroke =
                            (Color)Application.Current!
                                .Resources["InputBlue"];

                        outroCard.StrokeThickness =
                            1;
                    }
                }


                card.BackgroundColor =
                    Color.FromArgb(
                        "#EAF5FA");


                card.Stroke =
                    (Color)Application.Current!
                        .Resources["PrimaryBlue"];


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
        if (horarioSelecionado == null)
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
                    "MedicoId",
                    MedicoId
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
                    "HorarioId",
                    horarioSelecionado.IdHorario
                },

                {
                    "Data",
                    dataSelecionada
                },

                {
                    "Horario",
                    horarioSelecionado.HoraTexto
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