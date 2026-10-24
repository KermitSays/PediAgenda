
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
public partial class AgendarMedico : ContentPage
{
    private int pacienteId;
    private string? paciente;
    private string? especialidade;
    private Medico? medicoSelecionado;

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

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

    public string Especialidade
    {
        get => especialidade ?? string.Empty;

        set
        {
            especialidade = value;

            if (EspecialidadeLabel != null)
            {
                EspecialidadeLabel.Text =
                    $"Especialidade: {value}";
            }
        }
    }

    public AgendarMedico()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarMedicosAsync();
    }

    // CARREGA OS MÉDICOS REAIS DA API

    private async Task CarregarMedicosAsync()
    {
        try
        {
            medicoSelecionado = null;
            ErroLabel.IsVisible = false;

            MedicosCollectionView.ItemsSource = null;

            string? token = await SecureStorage.Default
                .GetAsync("pediagenda_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                MostrarErro(
                    "Faça login para consultar os médicos.");
                return;
            }

            string rota = "api/medicos";

            if (!string.IsNullOrWhiteSpace(Especialidade))
            {
                rota += "?especialidade=" +
                    Uri.EscapeDataString(Especialidade);
            }

            using var requisicao = new HttpRequestMessage(
                HttpMethod.Get,
                rota);

            requisicao.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    token);

            using var resposta =
                await http.SendAsync(requisicao);

            if (resposta.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                MostrarErro(
                    "Sua sessão expirou. Faça login novamente.");
                return;
            }

            if (!resposta.IsSuccessStatusCode)
            {
                MostrarErro(
                    "Não foi possível consultar os médicos.");
                return;
            }

            var dados = await resposta.Content
                .ReadFromJsonAsync<List<MedicoApiDto>>();

            var medicos = new List<Medico>();

            if (dados != null)
            {
                foreach (var item in dados)
                {
                    medicos.Add(new Medico
                    {
                        IdMedico = item.IdMedico,
                        Nome = item.Nome,
                        Crm = item.Especialidade,
                        Foto = "user.png"
                    });
                }
            }

            MedicosCollectionView.ItemsSource = medicos;

            if (medicos.Count == 0)
            {
                MostrarErro(
                    "Nenhum médico encontrado para esta especialidade.");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            MostrarErro(
                "Erro de conexão. Verifique se a API está funcionando.");
        }
    }

    private void MostrarErro(string mensagem)
    {
        ErroLabel.Text = mensagem;
        ErroLabel.IsVisible = true;
    }

    // SELECIONA O MÉDICO

    private void MedicoCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not Border border)
            return;

        if (border.BindingContext is not Medico medico)
            return;

        if (MedicosCollectionView.ItemsSource
            is not List<Medico> medicos)
            return;

        foreach (Medico item in medicos)
        {
            item.Selecionado = item == medico;
        }

        medicoSelecionado = medico;
        ErroLabel.IsVisible = false;
    }

    // CONTINUAR PARA DATA E HORÁRIO

    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (medicoSelecionado == null)
        {
            MostrarErro(
                "Selecione um médico para continuar.");
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(AgendarDataHorario),
            new Dictionary<string, object>
            {
                { "PacienteId", PacienteId },
                { "Paciente", Paciente },
                { "MedicoId", medicoSelecionado.IdMedico },
                { "Medico", medicoSelecionado.Nome },
                { "FotoMedico", medicoSelecionado.Foto },
                { "Especialidade", Especialidade }
            });
    }

    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // FORMATO DOS DADOS RETORNADOS PELA API

    private class MedicoApiDto
    {
        public int IdMedico { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Especialidade { get; set; } = string.Empty;
    }
}

// MODELO DO MÉDICO

public class Medico : INotifyPropertyChanged
{
    private bool selecionado;

    public int IdMedico { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Crm { get; set; } = string.Empty;

    public string Foto { get; set; } = string.Empty;

    public bool Selecionado
    {
        get => selecionado;

        set
        {
            if (selecionado == value)
                return;

            selecionado = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CorFundo));
            OnPropertyChanged(nameof(CorBorda));
            OnPropertyChanged(nameof(EspessuraBorda));
        }
    }

    public Color CorFundo =>
        Selecionado
            ? Color.FromArgb("#EAF5FA")
            : Color.FromArgb("#FFFFFF");

    public Color CorBorda =>
        Selecionado
            ? Color.FromArgb("#105A7E")
            : Color.FromArgb("#ABCCDC");

    public double EspessuraBorda =>
        Selecionado ? 2.5 : 1;

    public event PropertyChangedEventHandler?
        PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
