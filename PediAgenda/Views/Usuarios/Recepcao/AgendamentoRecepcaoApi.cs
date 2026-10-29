using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PediAgenda.Views.Usuarios.Recepcao;

public static class AgendamentoRecepcaoApi
{
    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public static async Task AgendarAsync(int idPaciente, int idHorario)
    {
        if (idPaciente <= 0 || idHorario <= 0)
            throw new InvalidOperationException("Selecione um paciente e um horário válidos.");

        string? token = await SecureStorage.Default.GetAsync("pediagenda_token");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Faça login novamente na Recepção.");

        using var req = new HttpRequestMessage(HttpMethod.Post, "api/recepcao/consultas")
        {
            Content = JsonContent.Create(new { idPaciente, idHorario })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var resp = await http.SendAsync(req);
        if (resp.IsSuccessStatusCode)
            return;

        var resposta = await resp.Content.ReadAsStringAsync();
        string erro = $"A API retornou erro HTTP {(int)resp.StatusCode}.";
        try
        {
            using var json = JsonDocument.Parse(resposta);
            if (json.RootElement.TryGetProperty("mensagem", out var mensagem) &&
                mensagem.ValueKind == JsonValueKind.String)
                erro = mensagem.GetString() ?? erro;
        }
        catch (JsonException) { }
        throw new InvalidOperationException(erro);
    }
}
