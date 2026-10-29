using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class PacientesResponsavel : ContentPage
{
    public ObservableCollection<PacienteResponsavelItem> PacientesLista
    {
        get;
        set;
    }

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public PacientesResponsavel()
    {
        InitializeComponent();

        PacientesLista = ResponsavelDados.Pacientes;

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarPacientesAsync();
    }

    // CARREGA OS DEPENDENTES DO RESPONSÁVEL LOGADO

    private async Task CarregarPacientesAsync()
    {
        try
        {
            string? token = await SecureStorage.Default
                .GetAsync("pediagenda_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlertAsync(
                    "Acesso não autorizado",
                    "Faça login para consultar seus dependentes.",
                    "OK");

                return;
            }

            using var requisicao = new HttpRequestMessage(
                HttpMethod.Get,
                "api/pacientes");

            requisicao.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var resposta = await http.SendAsync(requisicao);

            if (!resposta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Erro",
                    "Não foi possível consultar os dependentes.",
                    "OK");

                return;
            }

            var pacientes = await resposta.Content
                .ReadFromJsonAsync<List<PacienteApiDto>>();

            PacientesLista.Clear();

            if (pacientes != null)
            {
                foreach (var paciente in pacientes)
                {
                    if (!DateTime.TryParse(
                        paciente.DataNascimento,
                        out DateTime nascimento))
                    {
                        continue;
                    }

                    PacientesLista.Add(
                        new PacienteResponsavelItem
                        {
                            Id = paciente.Id,
                            Nome = paciente.Nome,
                            DataNascimento = nascimento
                        });
                }
            }

            // Atualiza a lista de dependentes
            PacientesCollectionView.ItemsSource = null;
            PacientesCollectionView.ItemsSource = PacientesLista;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Erro de conexão",
                "Não foi possível carregar os dependentes. Verifique se a API está funcionando.",
                "OK");
        }
    }

    // BOTÃO VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // DADOS DOS PACIENTES RECEBIDOS DA API

    private class PacienteApiDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string DataNascimento { get; set; } = string.Empty;

        public int Idade { get; set; }
    }
}