using PediAgenda.Dados;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class MenuResponsavel : ContentPage
{

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };  
    public MenuResponsavel()
    {
        InitializeComponent();
    }

    // =============================================
    // DADOS RECEBIDOS DA API
    // =============================================

    private class PacienteApiDto
    {
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;

        public string DataNascimento { get; set; } = string.Empty;
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

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AtualizarSaudacao();

        await CarregarDadosAsync();
   
    }

    // =============================================
    // CARREGAR PACIENTES E CONSULTAS DA API
    // =============================================

    private async Task CarregarDadosAsync()
    {
        try
        {
            string? token = await SecureStorage.Default
                .GetAsync("pediagenda_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlertAsync(
                    "Acesso não autorizado",
                    "Faça login novamente.",
                    "OK");
                return;
            }

            // BUSCAR PACIENTES
            using var reqPacientes = new HttpRequestMessage(
                HttpMethod.Get, "api/pacientes");

            reqPacientes.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var respPacientes = await http.SendAsync(reqPacientes);

            if (!respPacientes.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Erro",
                    "Não foi possível carregar os pacientes.",
                    "OK");
                return;
            }

            var listaPacientes = await respPacientes.Content
                .ReadFromJsonAsync<List<PacienteApiDto>>();

            // BUSCAR CONSULTAS
            using var reqConsultas = new HttpRequestMessage(
                HttpMethod.Get, "api/consultas");

            reqConsultas.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            using var respConsultas = await http.SendAsync(reqConsultas);

            if (!respConsultas.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Erro",
                    "Não foi possível carregar as consultas.",
                    "OK");
                return;
            }

            var listaConsultas = await respConsultas.Content
                .ReadFromJsonAsync<List<ConsultaApiDto>>();

            // ATUALIZAR PACIENTES
            ResponsavelDados.Pacientes.Clear();

            foreach (var item in listaPacientes ?? new List<PacienteApiDto>())
            {
                if (!DateTime.TryParse(
                    item.DataNascimento, out DateTime nascimento))
                    continue;

                ResponsavelDados.Pacientes.Add(
                    new PacienteResponsavelItem
                    {
                        Id = item.Id,
                        Nome = item.Nome,
                        DataNascimento = nascimento
                    });
            }

            // VINCULAR CONSULTAS AOS PACIENTES
            foreach (var item in listaConsultas ?? new List<ConsultaApiDto>())
            {
                var paciente = ResponsavelDados.Pacientes
                    .FirstOrDefault(p => p.Id == item.Paciente.Id);

                if (paciente == null)
                    continue;

                if (!DateTime.TryParse(item.Data, out DateTime data) ||
                    !TimeSpan.TryParse(item.Inicio, out TimeSpan horario))
                    continue;

                paciente.Consultas.Add(new ConsultaResponsavelItem
                {
                    Id = item.Id,
                    IdMedico = item.Medico.Id,
                    Medico = item.Medico.Nome,
                    Especialidade = item.Medico.Especialidade,
                    Data = data,
                    Horario = horario,
                    Status = item.Status
                });
            }

            // ATUALIZAR A LISTA DO MENU
            CarregarProximasConsultas();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Erro de conexão",
                "Não foi possível atualizar as consultas. Verifique se a API está funcionando.",
                "OK");
        }
    }

    // =============================================
    // SAUDAÇÃO
    // =============================================

    private void AtualizarSaudacao()
    {
        string nome =
            SessaoUsuario.PrimeiroNome;


        SaudacaoLabel.Text =
            string.IsNullOrWhiteSpace(nome)

                ? "Olá!"

                : $"Olá, {nome}!";
    }


    // =============================================
    // PRÓXIMA CONSULTA
    // =============================================

      private void CarregarProximasConsultas()
    {
        var proximas = ResponsavelDados.Pacientes
            .SelectMany(paciente =>
                paciente.Consultas.Select(consulta =>
                    new
                    {
                        Paciente = paciente,
                        Consulta = consulta
                    }))
            .Where(item =>
                item.Consulta.Data.Date >= DateTime.Today
                && !StatusConsulta.EhCancelada(item.Consulta.Status)
                && !StatusConsulta.EhRealizada(item.Consulta.Status))
            .OrderBy(item => item.Consulta.Data)
            .ThenBy(item => item.Consulta.Horario)
            .Take(3)
            .ToList();

        BindableLayout.SetItemsSource(
            ProximasConsultasContainer,
            proximas);

        ProximasConsultasContainer.IsVisible =
            proximas.Count > 0;

        SemConsultaCard.IsVisible =
            proximas.Count == 0;
    }

    // =============================================
    // ABRIR CONSULTA SELECIONADA
    // =============================================

    private async void ProximaConsulta_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not BindableObject elemento)
            return;

        if (elemento.BindingContext is null)
            return;

        dynamic item = elemento.BindingContext;

        ConsultaResponsavelItem consulta = item.Consulta;
        PacienteResponsavelItem paciente = item.Paciente;

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
        
    // =============================================
    // PACIENTES
    // =============================================

    private async void PacientesButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(PacientesResponsavel));
    }


    // =============================================
    // MINHAS CONSULTAS
    // =============================================

    private async void MinhasConsultasButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MinhasConsultas));
    }


    // =============================================
    // AGENDAR CONSULTA
    // =============================================

    private async void AgendarConsultaButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(AgendarConsulta));
    }
}