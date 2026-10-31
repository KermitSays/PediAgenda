using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class MinhasConsultas : ContentPage
{
    private ObservableCollection<PacienteResponsavelItem>
        pacientes;

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public MinhasConsultas()
    {
        InitializeComponent();


        pacientes =
            ResponsavelDados.Pacientes;


        PacientesCollectionView.ItemsSource =
            pacientes;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await CarregarPacientesAsync();
    }

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
                    "Faça login para consultar seus pacientes.",
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
                    "Não foi possível consultar os pacientes.",
                    "OK");
                return;
            }

            var lista = await resposta.Content
                .ReadFromJsonAsync<List<PacienteApiDto>>();

            pacientes.Clear();

            if (lista != null)
            {
                foreach (var paciente in lista)
                {
                    if (!DateTime.TryParse(
                        paciente.DataNascimento,
                        out DateTime nascimento))
                    {
                        continue;
                    }

                    pacientes.Add(new PacienteResponsavelItem
                    {
                        Id = paciente.Id,
                        Nome = paciente.Nome,
                        DataNascimento = nascimento
                    });
                }
            }

            // Busca as consultas agendadas de cada paciente
            await CarregarConsultasAsync(token);

            // Atualiza a lista de pacientes e consultas
            PacientesCollectionView.ItemsSource = null;
            PacientesCollectionView.ItemsSource = pacientes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Erro de conexão",
                "Não foi possível carregar os pacientes. Verifique se a API está funcionando.",
                "OK");
        }
    }

    private class PacienteApiDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string DataNascimento { get; set; } = string.Empty;

        public int Idade { get; set; }
    }

    private class ConsultaApiDto
    {
        public int Id { get; set; }

        public string Status { get; set; } = string.Empty;

        public string Data { get; set; } = string.Empty;

        public string Inicio { get; set; } = string.Empty;

        public string Fim { get; set; } = string.Empty;

        public MedicoApiDto Medico { get; set; } = new();

        public PacienteConsultaApiDto Paciente { get; set; } = new();
    }

    private class MedicoApiDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string Especialidade { get; set; } = string.Empty;
    }

    private class PacienteConsultaApiDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;
    }

    private async Task CarregarConsultasAsync(string token)
    {
        using var requisicao = new HttpRequestMessage(
            HttpMethod.Get,
            "api/consultas");

        requisicao.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var resposta = await http.SendAsync(requisicao);

        if (!resposta.IsSuccessStatusCode)
        {
            await DisplayAlertAsync(
                "Erro",
                "Não foi possível carregar as consultas.",
                "OK");
            return;
        }

        var consultas = await resposta.Content
            .ReadFromJsonAsync<List<ConsultaApiDto>>();

        if (consultas == null)
            return;

        foreach (var consulta in consultas)
        {
            PacienteResponsavelItem? paciente =
                pacientes.FirstOrDefault(p =>
                    p.Id == consulta.Paciente.Id);

            if (paciente == null)
                continue;

            // Verifica se a consulta já existe na lista do paciente
            if (paciente.Consultas.Any(c => c.Id == consulta.Id))
                continue;

            if (!DateTime.TryParse(consulta.Data, out DateTime data) ||
                !TimeSpan.TryParse(consulta.Inicio, out TimeSpan horario))
                continue;

            paciente.Consultas.Add(new ConsultaResponsavelItem
            {
                Id = consulta.Id,
                IdMedico = consulta.Medico.Id,
                Medico = consulta.Medico.Nome,
                Especialidade = consulta.Medico.Especialidade,
                Data = data,
                Horario = horario,
                Status = consulta.Status
            });
        }
    }

    // FILTROS DO HISTÓRICO DE CONSULTAS

    private void FiltroTodas_Clicked(object sender, EventArgs e)
    {
        AplicarFiltro(sender, "Todas");
    }

    private void FiltroAgendadas_Clicked(object sender, EventArgs e)
    {
        AplicarFiltro(sender, "Agendadas");
    }

    private void FiltroRealizadas_Clicked(object sender, EventArgs e)
    {
        AplicarFiltro(sender, "Realizadas");
    }

    private void FiltroCanceladas_Clicked(object sender, EventArgs e)
    {
        AplicarFiltro(sender, "Canceladas");
    }

    private void AplicarFiltro(object sender, string filtro)
    {
        // Identifica qual botão foi selecionado
        if (sender is not Button botao)
            return;

        // Identifica o paciente ao qual o botão pertence
        if (botao.BindingContext is not PacienteResponsavelItem paciente)
            return;

        // Atualiza o filtro do paciente
        paciente.FiltroSelecionado = filtro;
    }

    // ABRE / FECHA O PACIENTE

    private void PacienteCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is BindableObject elemento &&
            elemento.BindingContext
                is PacienteResponsavelItem paciente)
        {
            paciente.IsExpanded =
                !paciente.IsExpanded;
        }
    }


    /// Abre os detalhes da consulta conforme o status
    private async void ConsultaCard_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not BindableObject elemento ||
            elemento.BindingContext is not ConsultaResponsavelItem consulta)
            return;

        // Identifica o paciente da consulta selecionada
        PacienteResponsavelItem? paciente = pacientes.FirstOrDefault(p =>
            p.Consultas.Contains(consulta));

        if (paciente == null)
            return;

        // Consultas realizadas ou canceladas:
        // permite visualizar os detalhes, sem opção de cancelamento
        if (PediAgenda.Dados.StatusConsulta.EhRealizada(consulta.Status) ||
            PediAgenda.Dados.StatusConsulta.EhCancelada(consulta.Status))
        {
            await DisplayAlertAsync(
                "Detalhes da consulta",
                $"Paciente: {paciente.Nome}\n\n" +
                $"Médico: {consulta.Medico}\n" +
                $"Especialidade: {consulta.Especialidade}\n" +
                $"Data: {consulta.DataFormatada}\n" +
                $"Horário: {consulta.HorarioFormatado}\n" +
                $"Status: {consulta.Status}",
                "Fechar");

            return;
        }

        // Consultas agendadas ou confirmadas:
        // mantém a navegação para a tela de cancelamento
        await Shell.Current.GoToAsync(
            nameof(CancelarConsulta),
            new Dictionary<string, object>
            {
            { "ConsultaId", consulta.Id },
            { "Medico", consulta.Medico },
            { "Especialidade", consulta.Especialidade },
            { "Paciente", paciente.Nome },
            { "Data", consulta.DataFormatada },
            { "Horario", consulta.HorarioFormatado }
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