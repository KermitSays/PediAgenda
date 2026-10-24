using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(MedicoId), "MedicoId")]
[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
public partial class AgendarDataHorario : ContentPage
{
    private class HorariosApiResposta
    {
        public string Data { get; set; } = string.Empty;

        public List<MedicoHorariosApi> Medicos { get; set; } = new();
    }

    private class MedicoHorariosApi
    {
        public int IdMedico { get; set; }

        public string NomeMedico { get; set; } = string.Empty;

        public string Especialidade { get; set; } = string.Empty;

        public List<HorarioApi> Horarios { get; set; } = new();
    }

    private class HorarioApi
    {
        public int Id { get; set; }

        public string Inicio { get; set; } = string.Empty;

        public string Fim { get; set; } = string.Empty;
    }

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

    private async void CarregarHorarios()
    {
        if (HorariosContainer == null ||
            DataPicker == null ||
            MedicoId <= 0)
        {
            return;
        }

        HorariosContainer.Children.Clear();
        horarioSelecionado = null;

        DateTime dataSelecionada =
            DataPicker.Date ?? DateTime.Today;

        ErroHorarioLabel.Text = "Consultando horários disponíveis...";
        ErroHorarioLabel.IsVisible = true;

        try
        {
            string? token =
                await SecureStorage.Default.GetAsync("pediagenda_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                ErroHorarioLabel.Text =
                    "Sessão expirada. Faça login novamente.";
                return;
            }

            using HttpClient cliente = new HttpClient
            {
                BaseAddress = new Uri(
                    DeviceInfo.Platform == DevicePlatform.Android
                        ? "http://10.0.2.2:5000/"
                        : "http://localhost:5000/"),

                Timeout = TimeSpan.FromSeconds(20)
            };

            cliente.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            string data = dataSelecionada.ToString("yyyy-MM-dd");

            string url =
                $"api/horarios?data={data}&medico={MedicoId}";

            using HttpResponseMessage resposta =
                await cliente.GetAsync(url);

            if (!resposta.IsSuccessStatusCode)
            {
                ErroHorarioLabel.Text =
                    resposta.StatusCode == System.Net.HttpStatusCode.Unauthorized
                        ? "Sessão expirada. Faça login novamente."
                        : "Não foi possível consultar os horários.";
                return;
            }

            HorariosApiResposta? resultado =
                await resposta.Content.ReadFromJsonAsync<HorariosApiResposta>();

            string json = await resposta.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"RESPOSTA HORARIOS API: {json}");

            if (resultado == null)
            {
                ErroHorarioLabel.Text =
                    "Não foi possível obter os horários.";
                return;
            }

            List<HorarioAgendamentoItem> horariosDisponiveis = new();   

            foreach (MedicoHorariosApi medicoApi in resultado.Medicos)
            {
                foreach (HorarioApi horarioApi in medicoApi.Horarios)
                {
                    if (!TimeSpan.TryParse(horarioApi.Inicio, out TimeSpan inicio) ||
                        !TimeSpan.TryParse(horarioApi.Fim, out TimeSpan fim))
                    {
                        continue;
                    }

                    horariosDisponiveis.Add(
                        new HorarioAgendamentoItem
                        {
                            IdHorario = horarioApi.Id,
                            IdMedico = medicoApi.IdMedico,
                            Data = dataSelecionada,
                            HoraInicio = inicio,
                            HoraFim = fim,
                            Disponivel = true
                        });
                }
            }

            // Evita mostrar uma resposta antiga se a data
            // ou o médico forem alterados durante a consulta.
            if ((DataPicker.Date ?? DateTime.Today).Date != dataSelecionada.Date ||
                MedicoId <= 0)
            {
                return;
            }

            HorariosContainer.Children.Clear();

            if (horariosDisponiveis.Count == 0)
            {
                ErroHorarioLabel.Text =
                    "Não existem horários disponíveis para esta data.";
                ErroHorarioLabel.IsVisible = true;
                return;
            }

            ErroHorarioLabel.IsVisible = false;

            foreach (HorarioAgendamentoItem horario in
                     horariosDisponiveis.OrderBy(h => h.HoraInicio))
            {
                HorariosContainer.Children.Add(
                    CriarCardHorario(horario));
            }
        }
        catch (Exception)
        {
            ErroHorarioLabel.Text =
                "Erro ao consultar os horários. Verifique a conexão com a API.";
            ErroHorarioLabel.IsVisible = true;
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
                Text = horario.HoraTexto,

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
            nameof(AgendarConfirmacao),
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