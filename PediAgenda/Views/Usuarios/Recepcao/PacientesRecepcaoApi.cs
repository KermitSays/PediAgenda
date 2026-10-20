
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PediAgenda.Views.Usuarios.Recepcao;

public static class PacientesRecepcaoApi
{
#if DEBUG
#if ANDROID
    private const string EnderecoApi = "http://10.0.2.2:5000/";
#else
    private const string EnderecoApi = "http://localhost:5000/";
#endif
#else
    private const string EnderecoApi =
        "https://pediagenda-api.onrender.com/";
#endif

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri(EnderecoApi),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public static async Task<List<PacienteRecepcaoRespostaApi>>
        ListarAsync()
    {
        string? token = await SecureStorage.Default.GetAsync(
            "pediagenda_token");

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "É necessário entrar com as credenciais da Recepção.");
        }

        using var requisicao = new HttpRequestMessage(
            HttpMethod.Get,
            "api/recepcao/pacientes");

        requisicao.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        using var resposta = await http.SendAsync(requisicao);

        resposta.EnsureSuccessStatusCode();

        return await resposta.Content.ReadFromJsonAsync<
            List<PacienteRecepcaoRespostaApi>>()
            ?? new List<PacienteRecepcaoRespostaApi>();
    }
}

public sealed class PacienteRecepcaoRespostaApi
{
    public int Id { get; set; }

    public string Nome { get; set; } = "";

    public string DataNascimento { get; set; } = "";

    public string Responsavel { get; set; } = "";

    public string TelefoneResponsavel { get; set; } = "";

    public string? EmailResponsavel { get; set; }
}
