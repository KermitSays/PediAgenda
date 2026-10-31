using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(IdMedico), "IdMedico")]
public partial class LiberarHorarios : ContentPage
{
    public ObservableCollection<BloqueioExibicao> Bloqueios { get; } = new();

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    private int? idMedicoSelecionado;
    private string idMedico = string.Empty;
    private bool processando;
    private bool confirmando;
    private CancellationTokenSource? carregamento;

    protected override void OnDisappearing()
    {
        carregamento?.Cancel();
        base.OnDisappearing();
    }


    public string IdMedico
    {
        get => idMedico;
        set
        {
            idMedico = value ?? string.Empty;
            idMedicoSelecionado = int.TryParse(value, out int id) && id > 0
                ? id
                : null;
        }
    }

    public LiberarHorarios()
    {
        InitializeComponent();
        BloqueiosCollectionView.ItemsSource = Bloqueios;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarBloqueiosAsync();
    }

    private async Task CarregarBloqueiosAsync()
    {
        if (processando)
            return;

        processando = true;
        using var atual = new CancellationTokenSource();
        carregamento = atual;
        long sessao = SessaoUsuario.Versao;
        Bloqueios.Clear();
        BloqueiosCollectionView.IsVisible = false;
        EstadoLabel.Text = "Carregando bloqueios…";
        try
        {
            string token = await ObterTokenAsync();
            string rota = "api/agenda/periodos/bloqueados";
            if (idMedicoSelecionado.HasValue)
                rota += $"?medico={idMedicoSelecionado.Value}";

            using var requisicao = new HttpRequestMessage(HttpMethod.Get, rota);
            requisicao.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var resposta = await http.SendAsync(requisicao, atual.Token);
            if (!resposta.IsSuccessStatusCode)
                throw new InvalidOperationException(await LerErroAsync(resposta));

            var resultado = await resposta.Content
                .ReadFromJsonAsync<List<PeriodoBloqueadoApi>>(atual.Token)
                ?? throw new InvalidOperationException("A API retornou uma lista de bloqueios inválida.");

            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;

            Bloqueios.Clear();
            foreach (var periodo in resultado)
            {
                if (!DateTime.TryParseExact(periodo.Data, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data) ||
                    !TimeSpan.TryParseExact(periodo.Inicio, @"hh\:mm",
                    CultureInfo.InvariantCulture, out TimeSpan inicio) ||
                    !TimeSpan.TryParseExact(periodo.Fim, @"hh\:mm",
                    CultureInfo.InvariantCulture, out TimeSpan fim))
                    throw new InvalidOperationException("A API retornou um período inválido.");

                Bloqueios.Add(new BloqueioExibicao
                {
                    Data = data,
                    Inicio = inicio,
                    Fim = fim,
                    PeriodoData = data.ToString("dd/MM/yyyy"),
                    PeriodoHorario = inicio.ToString(@"hh\:mm") + " às " + fim.ToString(@"hh\:mm")
                });
            }
            EstadoLabel.Text = Bloqueios.Count == 0 ? "Nenhum período bloqueado." : "Bloqueios da agenda";
            BloqueiosCollectionView.IsVisible = true;
        }
        catch (Exception ex)
        {
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            Bloqueios.Clear();
            EstadoLabel.Text = "Não foi possível carregar. Volte à agenda e abra esta tela novamente.";
            await DisplayAlertAsync("Não foi possível carregar os bloqueios", ex.Message, "OK");
        }
        finally
        {
            processando = false;
            if (ReferenceEquals(carregamento, atual)) carregamento = null;
        }
    }

    private async void LiberarPeriodoButton_Clicked(object? sender, EventArgs e)
    {
        if (processando || confirmando || sender is not Button button ||
            button.BindingContext is not BloqueioExibicao item)
            return;

        long sessao = SessaoUsuario.Versao;
        confirmando = true;
        bool confirmar;
        try
        {
            confirmar = await DisplayAlertAsync(
            "Liberar período",
            $"Deseja liberar todo este período?\n\n" +
            $"Data: {item.PeriodoData}\n" +
            $"Horário: {item.PeriodoHorario}\n\n" +
            "As consultas canceladas pelo bloqueio não serão restauradas.",
            "Liberar", "Cancelar");
        }
        finally { confirmando = false; }

        if (!confirmar || sessao != SessaoUsuario.Versao) return;

        processando = true;
        try
        {
            string token = await ObterTokenAsync();
            if (sessao != SessaoUsuario.Versao) return;
            using var requisicao = new HttpRequestMessage(
                HttpMethod.Post, "api/agenda/periodos/liberar")
            {
                Content = JsonContent.Create(new
                {
                    idMedico = idMedicoSelecionado,
                    dataInicial = item.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    dataFinal = item.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    horarioInicial = item.Inicio.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                    horarioFinal = item.Fim.ToString(@"hh\:mm", CultureInfo.InvariantCulture)
                })
            };
            requisicao.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var resposta = await http.SendAsync(requisicao);
            if (!resposta.IsSuccessStatusCode)
                throw new InvalidOperationException(await LerErroAsync(resposta));

            if (sessao != SessaoUsuario.Versao) return;
            Bloqueios.Clear();
            BloqueiosCollectionView.IsVisible = false;

            await DisplayAlertAsync("Período liberado",
                "O período foi liberado com sucesso.", "OK");
        }
        catch (Exception ex)
        {
            if (sessao != SessaoUsuario.Versao) return;
            await DisplayAlertAsync("Não foi possível liberar o período", ex.Message, "OK");
            return;
        }
        finally
        {
            processando = false;
        }

        await CarregarBloqueiosAsync();
    }

    private static async Task<string> ObterTokenAsync()
    {
        string? token = await SecureStorage.Default.GetAsync("pediagenda_token");
        return string.IsNullOrWhiteSpace(token)
            ? throw new InvalidOperationException("É necessário fazer login novamente.")
            : token;
    }

    private static async Task<string> LerErroAsync(HttpResponseMessage resposta)
    {
        string texto = await resposta.Content.ReadAsStringAsync();
        try
        {
            using var json = JsonDocument.Parse(texto);
            if (json.RootElement.TryGetProperty("mensagem", out var mensagem) &&
                mensagem.ValueKind == JsonValueKind.String)
                return mensagem.GetString() ?? "Falha na operação.";
        }
        catch (JsonException)
        {
        }

        return $"A API retornou erro HTTP {(int)resposta.StatusCode}.";
    }

    private async void VoltarButton_Clicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private sealed class PeriodoBloqueadoApi
    {
        public PeriodoBloqueadoApi() { }
        public string Data { get; set; } = string.Empty;
        public string Inicio { get; set; } = string.Empty;
        public string Fim { get; set; } = string.Empty;
        public int QuantidadeHorarios { get; set; }
    }

    public class BloqueioExibicao
    {
        public DateTime Data { get; set; }
        public TimeSpan Inicio { get; set; }
        public TimeSpan Fim { get; set; }
        public string PeriodoData { get; set; } = string.Empty;
        public string PeriodoHorario { get; set; } = string.Empty;
    }
}

