namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(ConsultaId), "ConsultaId")]
[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(Paciente), "Paciente")]
public partial class ReagendarConsulta : ContentPage
{
    private int consultaId;

    private string? medico;

    private string? especialidade;

    private string? paciente;

    private HorarioAgendamentoItem?
        horarioSelecionado;

    private ConsultaResponsavelItem?
        consultaAtual;

    private bool ajustandoData;

    private bool reagendamentoConcluido;


    public int ConsultaId
    {
        get => consultaId;

        set
        {
            consultaId = value;

            LocalizarConsulta();
        }
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


    public string Paciente
    {
        get => paciente ?? string.Empty;

        set
        {
            paciente = value;


            if (PacienteLabel != null)
            {
                PacienteLabel.Text =
                    value;
            }


            LocalizarConsulta();
        }
    }


    public ReagendarConsulta()
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


        if (reagendamentoConcluido)
            return;


        LocalizarConsulta();


        CarregarHorarios();
    }


    // LOCALIZA A CONSULTA

    private void LocalizarConsulta()
    {
        if (ConsultaId <= 0)
            return;


        PacienteResponsavelItem? pacienteEncontrado =
            ResponsavelDados.Pacientes
                .FirstOrDefault(p =>
                    p.Consultas.Any(c =>
                        c.Id == ConsultaId));


        if (pacienteEncontrado == null)
            return;


        consultaAtual =
            pacienteEncontrado.Consultas
                .FirstOrDefault(c =>
                    c.Id == ConsultaId);
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


    // CARREGA HORÁRIOS DISPONÍVEIS

    private void CarregarHorarios()
    {
        if (HorariosContainer == null ||
            DataPicker == null)
        {
            return;
        }


        HorariosContainer.Children.Clear();


        horarioSelecionado =
            null;


        LocalizarConsulta();


        if (consultaAtual == null)
        {
            ErroHorarioLabel.Text =
                "Não foi possível localizar a consulta.";


            ErroHorarioLabel.IsVisible =
                true;


            return;
        }


        if (consultaAtual.IdMedico <= 0)
        {
            ErroHorarioLabel.Text =
                "Não foi possível identificar o médico desta consulta.";


            ErroHorarioLabel.IsVisible =
                true;


            return;
        }


        DateTime dataSelecionada =
            DataPicker.Date
            ?? DateTime.Today;


        List<HorarioAgendamentoItem>
            horariosDisponiveis =
                HorarioAgendamentoDados
                    .ObterHorariosDisponiveis(
                        consultaAtual.IdMedico,
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
                    if (item is Border outroCard)
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
                        "#E8F3FF");


                card.Stroke =
                    (Color)Application.Current!
                        .Resources["PrimaryBlue"];


                card.StrokeThickness =
                    2;


                ErroHorarioLabel.IsVisible =
                    false;
            };


        card.GestureRecognizers.Add(
            tap);


        return card;
    }


    // CONFIRMA O REAGENDAMENTO

    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (reagendamentoConcluido)
            return;


        if (horarioSelecionado == null)
        {
            ErroHorarioLabel.Text =
                "Selecione um horário para continuar.";


            ErroHorarioLabel.IsVisible =
                true;


            return;
        }


        LocalizarConsulta();


        if (consultaAtual == null)
        {
            await DisplayAlertAsync(
                "Erro",
                "Não foi possível localizar a consulta.",
                "OK");


            return;
        }


        if (consultaAtual.Status.Equals(
            "Cancelada",
            StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Consulta cancelada",
                "Uma consulta cancelada não pode ser reagendada.",
                "OK");


            return;
        }


        if (consultaAtual.Status.Equals(
            "Realizada",
            StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Consulta realizada",
                "Uma consulta já realizada não pode ser reagendada.",
                "OK");


            return;
        }


        DateTime novaData =
            DataPicker.Date
            ?? DateTime.Today;


        bool confirmar =
            await DisplayAlertAsync(
                "Confirmar reagendamento",
                $"Deseja reagendar a consulta para " +
                $"{novaData:dd/MM/yyyy} às " +
                $"{horarioSelecionado.HoraTexto}?",
                "SIM",
                "NÃO");


        if (!confirmar)
            return;


        int idHorarioAntigo =
            consultaAtual.IdHorario;


        // RESERVA PRIMEIRO O NOVO HORÁRIO

        bool novoHorarioReservado =
            HorarioAgendamentoDados
                .ReservarHorario(
                    horarioSelecionado.IdHorario);


        if (!novoHorarioReservado)
        {
            await DisplayAlertAsync(
                "Horário indisponível",
                "Este horário não está mais disponível. " +
                "Escolha outro horário.",
                "OK");


            CarregarHorarios();


            return;
        }


        // LIBERA O HORÁRIO ANTIGO

        if (idHorarioAntigo > 0)
        {
            HorarioAgendamentoDados
                .LiberarHorario(
                    idHorarioAntigo);
        }


        // ATUALIZA A CONSULTA

        consultaAtual.IdHorario =
            horarioSelecionado.IdHorario;


        consultaAtual.Data =
            novaData;


        consultaAtual.Horario =
            horarioSelecionado.HoraInicio;


        consultaAtual.Status =
            "Por confirmar";


        // NOTIFICAÇÃO

        NotificacoesDados.Notificacoes.Add(
            new Notificacao
            {
                Titulo =
                    "Consulta reagendada",

                Mensagem =
                    $"A consulta de {Paciente} com {Medico} " +
                    $"foi reagendada para " +
                    $"{consultaAtual.Data:dd/MM/yyyy} às " +
                    $"{consultaAtual.Horario:hh\\:mm}.",

                DataHora =
                    DateTime.Now,

                Lida =
                    false
            });


        reagendamentoConcluido =
            true;


        // PREENCHE A TELA FINAL

        NovaDataLabel.Text =
            consultaAtual.Data.ToString(
                "dd/MM/yyyy");


        NovoHorarioLabel.Text =
            consultaAtual.Horario.ToString(
                @"hh\:mm");


        ReagendamentoContainer.IsVisible =
            false;


        ConclusaoContainer.IsVisible =
            true;
    }


    // MINHAS CONSULTAS

    private async void MinhasConsultasButton_Clicked(
        object sender,
        EventArgs e)
    {
        await NavegacaoResponsavel
            .IrParaMinhasConsultasAsync();
    }


    // MENU

    private async void MenuButton_Clicked(
        object sender,
        EventArgs e)
    {
        await NavegacaoResponsavel
            .IrParaMenuAsync();
    }


    // VOLTAR ANTES DE REAGENDAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}