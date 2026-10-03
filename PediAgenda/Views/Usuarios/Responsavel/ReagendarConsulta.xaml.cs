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

    private string? horarioSelecionado;


    public int ConsultaId
    {
        get => consultaId;

        set => consultaId = value;
    }


    public string Medico
    {
        get => medico ?? string.Empty;

        set
        {
            medico = value;

            if (MedicoLabel != null)
            {
                MedicoLabel.Text = value;
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
                EspecialidadeLabel.Text = value;
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
                PacienteLabel.Text = value;
            }
        }
    }


    public ReagendarConsulta()
    {
        InitializeComponent();

        DataPicker.MinimumDate =
            DateTime.Today;

        DataPicker.Date =
            DateTime.Today;

        CarregarHorarios();
    }


    // ALTERA A DATA
    private void DataPicker_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        horarioSelecionado = null;

        CarregarHorarios();

        ErroHorarioLabel.IsVisible = false;
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

                StrokeThickness = 1,

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
                Text = horario,

                FontSize = 16,

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
                    Color.FromArgb("#E8F3FF");

                card.Stroke =
                    (Color)Application.Current.Resources["PrimaryBlue"];

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
        if (string.IsNullOrEmpty(
            horarioSelecionado))
        {
            ErroHorarioLabel.Text =
                "Selecione um horário para continuar.";

            ErroHorarioLabel.IsVisible =
                true;

            return;
        }


        // Localiza o paciente nos dados compartilhados
        PacienteResponsavelItem? pacienteEncontrado =
            ResponsavelDados.Pacientes
                .FirstOrDefault(p =>
                    p.Nome.Equals(
                        Paciente,
                        StringComparison.OrdinalIgnoreCase));


        if (pacienteEncontrado == null)
        {
            await DisplayAlertAsync(
                "Erro",
                "Não foi possível localizar o paciente.",
                "OK");

            return;
        }


        // Localiza a consulta pelo ID
        ConsultaResponsavelItem? consulta =
            pacienteEncontrado.Consultas
                .FirstOrDefault(c =>
                    c.Id == ConsultaId);


        if (consulta == null)
        {
            await DisplayAlertAsync(
                "Erro",
                "Não foi possível localizar a consulta.",
                "OK");

            return;
        }


        bool confirmar =
            await DisplayAlertAsync(
                "Confirmar reagendamento",
                $"Deseja reagendar a consulta para " +
                $"{DataPicker.Date:dd/MM/yyyy} às " +
                $"{horarioSelecionado}?",
                "SIM",
                "NÃO");


        if (!confirmar)
            return;


        // Atualiza a consulta existente
        consulta.Data =
            DataPicker.Date ?? DateTime.Today;


        if (TimeSpan.TryParse(
            horarioSelecionado,
            out TimeSpan novoHorario))
        {
            consulta.Horario =
                novoHorario;
        }


        // Após um reagendamento,
        // a consulta volta para confirmação
        consulta.Status =
            "Por confirmar";


        // Cria notificação para o responsável
        NotificacoesDados.Notificacoes.Add(
            new Notificacao
            {
                Titulo =
                    "Consulta reagendada",

                Mensagem =
                    $"A consulta de {Paciente} com {Medico} " +
                    $"foi reagendada para " +
                    $"{consulta.Data:dd/MM/yyyy} às " +
                    $"{consulta.Horario:hh\\:mm}.",

                DataHora =
                    DateTime.Now,

                Lida =
                    false
            });


        await DisplayAlertAsync(
            "Reagendamento concluído",
            $"Consulta reagendada para " +
            $"{consulta.Data:dd/MM/yyyy} às " +
            $"{consulta.Horario:hh\\:mm}.",
            "OK");


        await Shell.Current.GoToAsync(
            nameof(MinhasConsultas));
    }


    // VOLTAR
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}