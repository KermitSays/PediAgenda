using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace PediAgenda.Views.Usuarios.Recepcao;

public sealed class ConsultaRecepcaoRespostaApi
{
    public int Id { get; set; }
    public string Paciente { get; set; } = "";
    public string Responsavel { get; set; } = "";
    public string Medico { get; set; } = "";
    public string Data { get; set; } = "";
    public string Inicio { get; set; } = "";
    public string Status { get; set; } = "";
}

public static class RecepcaoGestaoConsultasApi
{
    // Mesma API local utilizada pelas demais telas da Recepção neste estágio.
    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public static async Task<ConsultaRecepcaoRespostaApi> BuscarAsync(int id)
    {
        using var resposta = await EnviarAsync(HttpMethod.Get, $"api/recepcao/consultas/{id}");
        return await resposta.Content.ReadFromJsonAsync<ConsultaRecepcaoRespostaApi>()
            ?? throw new InvalidOperationException("A API não retornou os detalhes da consulta.");
    }

    public static async Task ConfirmarAsync(int id)
    {
        // Rota existente na API, compartilhada entre Médico e Recepção.
        using var _ = await EnviarAsync(HttpMethod.Patch, $"api/consultas/{id}/confirmar");
    }

    public static async Task CancelarAsync(int id)
    {
        using var _ = await EnviarAsync(HttpMethod.Patch, $"api/recepcao/consultas/{id}/cancelar");
    }

    private static async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string rota)
    {
        var token = await SecureStorage.Default.GetAsync("pediagenda_token");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Sessão expirada. Faça login novamente na Recepção.");

        using var requisicao = new HttpRequestMessage(metodo, rota);
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resposta = await http.SendAsync(requisicao);
        if (resposta.IsSuccessStatusCode)
            return resposta;

        string mensagem = $"A API retornou erro {(int)resposta.StatusCode}.";
        try
        {
            string conteudo = await resposta.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(conteudo);
            if (json.RootElement.TryGetProperty("mensagem", out var campo))
                mensagem = campo.GetString() ?? mensagem;
        }
        catch (JsonException) { /* Usa a mensagem do status HTTP. */ }
        finally { resposta.Dispose(); }
        throw new InvalidOperationException(mensagem);
    }
}
