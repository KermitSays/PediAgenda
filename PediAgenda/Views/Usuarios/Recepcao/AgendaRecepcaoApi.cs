
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PediAgenda.Views.Usuarios.Recepcao;

public static class AgendaRecepcaoApi
{
    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    // CONSULTAR AGENDA REAL

    public static async Task<AgendaRecepcaoResposta> ConsultarAsync(
        DateTime data,
        int? idMedico = null)
    {
        string? token = await SecureStorage.Default.GetAsync(
            "pediagenda_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "É necessário fazer login novamente.");
        }

        string rota = $"api/agenda?data={data:yyyy-MM-dd}";

        if (idMedico > 0)
        {
            rota += $"&medico={idMedico}";
        }

        using var requisicao = new HttpRequestMessage(
            HttpMethod.Get, rota);

        requisicao.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var resposta = await http.SendAsync(requisicao);

        resposta.EnsureSuccessStatusCode();

        return await resposta.Content
            .ReadFromJsonAsync<AgendaRecepcaoResposta>()
            ?? new AgendaRecepcaoResposta();
    }

    // LISTAR MÉDICOS REAIS

    public static async Task<List<MedicoRecepcaoResposta>> ListarMedicosAsync()
    {
        string? token = await SecureStorage.Default.GetAsync(
            "pediagenda_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "É necessário fazer login novamente.");
        }

        using var requisicao = new HttpRequestMessage(
            HttpMethod.Get, "api/medicos");

        requisicao.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var resposta = await http.SendAsync(requisicao);

        resposta.EnsureSuccessStatusCode();

        return await resposta.Content
            .ReadFromJsonAsync<List<MedicoRecepcaoResposta>>()
            ?? new List<MedicoRecepcaoResposta>();
    }

    // BLOQUEAR HORÁRIO REAL

    public static async Task BloquearHorarioAsync(int idHorario)
    {
        await AlterarBloqueioAsync(idHorario, true);
    }

    // LIBERAR HORÁRIO REAL

    public static async Task DesbloquearHorarioAsync(int idHorario)
    {
        await AlterarBloqueioAsync(idHorario, false);
    }

    // ENVIAR BLOQUEIO OU LIBERAÇÃO PARA A API

    private static async Task AlterarBloqueioAsync(
        int idHorario,
        bool bloquear)
    {
        if (idHorario <= 0)
        {
            throw new ArgumentException(
                "O horário selecionado é inválido.");
        }

        string? token = await SecureStorage.Default.GetAsync(
            "pediagenda_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "É necessário fazer login novamente.");
        }

        string acao = bloquear
            ? "bloquear"
            : "desbloquear";

        string rota = $"api/horarios/{idHorario}/{acao}";

        using var requisicao = new HttpRequestMessage(
            HttpMethod.Patch, rota);

        requisicao.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var resposta = await http.SendAsync(requisicao);

        resposta.EnsureSuccessStatusCode();
    }
}

// RESPOSTA DA AGENDA

public class AgendaRecepcaoResposta
{
    public string Data { get; set; } = "";

    public List<MedicoAgendaRecepcaoResposta> Medicos { get; set; } = new();
}

// MÉDICO DA AGENDA

public class MedicoAgendaRecepcaoResposta
{
    public int Id { get; set; }

    public string Nome { get; set; } = "";

    public string Especialidade { get; set; } = "";

    public List<HorarioAgendaRecepcaoResposta> Horarios { get; set; } = new();
}

// HORÁRIO DA AGENDA

public class HorarioAgendaRecepcaoResposta
{
    public int Id { get; set; }

    public string Inicio { get; set; } = "";

    public string Fim { get; set; } = "";

    public string Situacao { get; set; } = "";

    public ConsultaAgendaRecepcaoResposta? Consulta { get; set; }
}

// CONSULTA DA AGENDA

public class ConsultaAgendaRecepcaoResposta
{
    public int Id { get; set; }

    public string Status { get; set; } = "";

    public PacienteAgendaRecepcaoResposta Paciente { get; set; } = new();
}

// PACIENTE DA CONSULTA

public class PacienteAgendaRecepcaoResposta
{
    public int Id { get; set; }

    public string Nome { get; set; } = "";
}

// MÉDICO DA LISTAGEM

public class MedicoRecepcaoResposta
{
    public int IdMedico { get; set; }

    public string Nome { get; set; } = "";

    public string Especialidade { get; set; } = "";
}
