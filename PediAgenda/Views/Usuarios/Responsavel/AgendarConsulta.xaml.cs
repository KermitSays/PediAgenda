
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class AgendarConsulta : ContentPage
{
    private PacienteResponsavelItem? pacienteSelecionado;
    private string? especialidadeSelecionada;

    private readonly ObservableCollection<PacienteResponsavelItem>
        pacientesDisponiveis = new();

    private bool carregandoPacientes;

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public AgendarConsulta()
    {
        InitializeComponent();
    }

    // CARREGA OS DEPENDENTES PELA API

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarPacientesAsync();
    }

    private async Task CarregarPacientesAsync()
    {
        if (carregandoPacientes)
            return;

        carregandoPacientes = true;

        pacienteSelecionado = null;
        PacientePicker.SelectedIndex = -1;
        PacientePicker.Items.Clear();
        pacientesDisponiveis.Clear();

        try
        {
            string? token = await SecureStorage.Default
                .GetAsync("pediagenda_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                MostrarErro(
                    "Faça login para consultar seus pacientes.");
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
                MostrarErro(
                    "Não foi possível carregar os pacientes.");
                return;
            }

            var pacientes = await resposta.Content
                .ReadFromJsonAsync<List<PacienteApiDto>>();

            if (pacientes == null || pacientes.Count == 0)
            {
                MostrarErro(
                    "Nenhum paciente vinculado ao seu cadastro.");
                return;
            }

            foreach (var paciente in pacientes)
            {
                if (!DateTime.TryParse(
                    paciente.DataNascimento,
                    out DateTime nascimento))
                {
                    continue;
                }

                pacientesDisponiveis.Add(
                    new PacienteResponsavelItem
                    {
                        Id = paciente.Id,
                        Nome = paciente.Nome,
                        DataNascimento = nascimento
                    });

                PacientePicker.Items.Add(paciente.Nome);
            }

            if (pacientesDisponiveis.Count == 0)
            {
                MostrarErro(
                    "Não foi possível carregar os dados dos pacientes.");
            }
            else
            {
                ErroLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            MostrarErro(
                "Erro de conexão. Verifique se a API está funcionando.");
        }
        finally
        {
            carregandoPacientes = false;
        }
    }

    // PACIENTE SELECIONADO

    private void PacientePicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        int indice = PacientePicker.SelectedIndex;

        if (indice < 0 || indice >= pacientesDisponiveis.Count)
        {
            pacienteSelecionado = null;
            return;
        }

        pacienteSelecionado = pacientesDisponiveis[indice];
        ErroLabel.IsVisible = false;
    }

    // ESPECIALIDADE SELECIONADA

    private void EspecialidadePicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (EspecialidadePicker.SelectedIndex >= 0)
        {
            especialidadeSelecionada =
                EspecialidadePicker.Items[
                    EspecialidadePicker.SelectedIndex];

            ErroLabel.IsVisible = false;
        }
        else
        {
            especialidadeSelecionada = null;
        }
    }

    // CONTINUAR

    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (pacienteSelecionado == null)
        {
            MostrarErro(
                "Selecione o paciente para continuar.");
            return;
        }

        if (string.IsNullOrWhiteSpace(especialidadeSelecionada))
        {
            MostrarErro(
                "Selecione uma especialidade para continuar.");
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(AgendarMedico),
            new Dictionary<string, object>
            {
                { "PacienteId", pacienteSelecionado.Id },
                { "Paciente", pacienteSelecionado.Nome },
                { "Especialidade", especialidadeSelecionada }
            });
    }

    // MENSAGEM DE ERRO

    private void MostrarErro(string mensagem)
    {
        ErroLabel.Text = mensagem;
        ErroLabel.IsVisible = true;
    }

    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // MODELO DA RESPOSTA DA API

    private class PacienteApiDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string DataNascimento { get; set; } = string.Empty;

        public int Idade { get; set; }
    }
}
