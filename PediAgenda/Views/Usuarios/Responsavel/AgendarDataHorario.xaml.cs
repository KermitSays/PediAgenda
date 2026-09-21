namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
public partial class AgendarDataHorario : ContentPage
{
    // Guarda o médico recebido da tela anterior.
    private string? medico;

    // Guarda a foto do médico recebida da tela anterior.
    private string? fotoMedico;

    // Guarda a especialidade recebida da tela anterior.
    private string? especialidade;

    // Guarda o horário escolhido pelo usuário.
    private string? horarioSelecionado;

    // Evita que o evento de mudança de data seja chamado várias vezes
    private bool ajustandoData;



    // MÉDICO

    public string Medico
    {
        get => medico ?? string.Empty;

        set
        {
            medico = value;

            // Atualiza o nome do médico na tela.
            if (MedicoLabel != null)
            {
                MedicoLabel.Text = value;
            }
        }
    }

    public string FotoMedico
    {
        get => fotoMedico ?? string.Empty;

        set => fotoMedico = value;
    }


    // ESPECIALIDADE

    public string Especialidade
    {
        get => especialidade ?? string.Empty;

        set
        {
            especialidade = value;

            // Atualiza a especialidade na tela.
            if (EspecialidadeLabel != null)
            {
                EspecialidadeLabel.Text = value;
            }
        }
    }



    // CONSTRUTOR

    public AgendarDataHorario()
    {
        InitializeComponent();

        DataPicker.MinimumDate = DateTime.Today;
        DataPicker.Date = ProximaDataDisponivel();

        CarregarHorarios();
    }

    // Retorna a próxima data disponível para agendamento, ignorando finais de semana.
    private DateTime ProximaDataDisponivel()
    {
        DateTime data = DateTime.Today;

        while (data.DayOfWeek == DayOfWeek.Saturday ||
               data.DayOfWeek == DayOfWeek.Sunday)
        {
            data = data.AddDays(1);
        }

        return data;
    }


    // QUANDO A DATA É ALTERADA

    private void DataPicker_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        if (ajustandoData)
            return;

        if (e.NewDate.HasValue &&
            (e.NewDate.Value.DayOfWeek == DayOfWeek.Saturday ||
             e.NewDate.Value.DayOfWeek == DayOfWeek.Sunday))
        {
            ErroHorarioLabel.Text =
                "A clínica não atende aos finais de semana.";

            ErroHorarioLabel.IsVisible = true;

            ajustandoData = true;

            DataPicker.Date = ProximaDataDisponivel();

            ajustandoData = false;

            return;
        }

        horarioSelecionado = null;

        CarregarHorarios();

        ErroHorarioLabel.IsVisible = false;
    }


    // CARREGAR HORÁRIOS

    private void CarregarHorarios()
    {
        // Remove os cards antigos antes de criar os novos.
        HorariosContainer.Children.Clear();


        // Lista de horários disponíveis.
        
        // Por enquanto eles estão fixos. Futuramente virão do banco/API.
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


        // Percorre todos os horários da lista.
        foreach (string horario in horarios)
        {
            // Para cada horário, cria um card.
            HorariosContainer.Children.Add(
                CriarCardHorario(horario));
        }
    }


    // CRIAR CARD DE HORÁRIO

    // Este método cria visualmente um card para cada horário.
    private Border CriarCardHorario(string horario)
    {
        // Cria o Border que será o nosso card.
        Border card = new Border
        {
            // Cor normal do fundo.
            BackgroundColor =
                (Color)Application.Current.Resources["White"],

            // Cor normal da borda.
            Stroke =
                (Color)Application.Current.Resources["InputBlue"],

            // Espessura normal da borda.
            StrokeThickness = 1,

            // Espaçamento interno do card.
            Padding = new Thickness(18, 12),

            // Espaçamento entre os cards.
            Margin = new Thickness(0, 0, 10, 10),

            // Deixa os cantos arredondados.
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = new CornerRadius(12)
            }
        };


        // TEXTO DO HORÁRIO

        // Cria o Label que mostrará "08:00", "08:30" etc.
        Label horarioLabel = new Label
        {
            Text = horario,

            FontSize = 16,

            FontAttributes = FontAttributes.Bold,

            TextColor =
                (Color)Application.Current.Resources["TextDark"],

            HorizontalOptions = LayoutOptions.Center,

            VerticalOptions = LayoutOptions.Center
        };


        // Coloca o texto dentro do card.
        card.Content = horarioLabel;


       
        // TOQUE NO CARD

        // Cria o gesto que detecta quando o usuário toca no card.
        TapGestureRecognizer tap =
            new TapGestureRecognizer();


        // O código abaixo será executado quando o card for tocado.
        tap.Tapped += (sender, e) =>
        {
            // Guarda o horário que o usuário escolheu.
            horarioSelecionado = horario;


            // Percorre todos os cards de horário.
            foreach (View item in HorariosContainer.Children)
            {
                // Verifica se o item é realmente um Border/card.
                if (item is Border outroCard)
                {
                    // Volta todos os outros cards para
                    // a aparência normal.
                    outroCard.BackgroundColor =
                        (Color)Application.Current.Resources["White"];

                    outroCard.Stroke =
                        (Color)Application.Current.Resources["InputBlue"];

                    outroCard.StrokeThickness = 1;
                }
            }


            
            // CARD SELECIONADO

            // Fundo azul clarinho.
            card.BackgroundColor =
                Color.FromArgb("#EAF5FA");

            // Borda azul mais forte.
            card.Stroke =
                (Color)Application.Current.Resources["PrimaryBlue"];

            // Borda um pouco mais grossa.
            card.StrokeThickness = 2.5;


            // Como o usuário escolheu um horário,
            // escondemos a mensagem de erro.
            ErroHorarioLabel.IsVisible = false;
        };


        // Adiciona o gesto de toque ao card.
        card.GestureRecognizers.Add(tap);


        // Devolve o card pronto para ser colocado na tela.
        return card;
    }

    // BOTÃO CONTINUAR
    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        // Verifica se o usuário escolheu algum horário.
        if (string.IsNullOrEmpty(horarioSelecionado))
        {
            // Mostra a mensagem de erro.
            ErroHorarioLabel.Text =
                "Selecione um horário para continuar.";

            ErroHorarioLabel.IsVisible = true;

            return;
        }

        string dataSelecionada =
            ((DateTime)DataPicker.Date).ToString("dd/MM/yyyy");

        await Shell.Current.GoToAsync(
            nameof(AgendarTipoAtendimento),
            new Dictionary<string, object>
            {
                { "Medico", Medico },
                { "FotoMedico", FotoMedico },
                { "Especialidade", Especialidade },
                { "Data", dataSelecionada },
                { "Horario", horarioSelecionado }
            });
    }


    // BOTÃO VOLTAR
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        // Volta para a página anterior.
        await Shell.Current.GoToAsync("..");
    }
}