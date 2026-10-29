using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

// Os IDs da API são inteiros; não devem ser misturados com os GUIDs do teste visual.
internal static class MedicoApi
{
    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    public static bool ModoTeste => SessaoUsuario.IdUsuario == 0
        && SessaoUsuario.Perfil == "MEDICO"
        && SessaoUsuario.Nome == "Médico Teste";

    private static async Task<HttpResponseMessage> EnviarAsync(HttpRequestMessage pedido,
        CancellationToken cancellationToken = default)
    {
        if (ModoTeste || SessaoUsuario.IdUsuario <= 0 || SessaoUsuario.Perfil != "MEDICO")
            throw new InvalidOperationException("Entre com uma conta de médico para acessar os dados da clínica.");

        string? token = await SecureStorage.Default.GetAsync("pediagenda_token");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Sua sessão expirou. Entre novamente com e-mail e senha.");

        pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        HttpResponseMessage resposta;
        try
        {
            resposta = await http.SendAsync(pedido, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException("Não foi possível conectar à API. Verifique se ela está em execução e tente novamente.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("A API demorou para responder. Atualize a agenda antes de tentar novamente.");
        }

        if (resposta.IsSuccessStatusCode)
            return resposta;

        using (resposta)
        {
            if (resposta.StatusCode == HttpStatusCode.Unauthorized)
                throw new InvalidOperationException("Sua sessão expirou. Entre novamente com e-mail e senha.");
            if (resposta.StatusCode == HttpStatusCode.Forbidden)
                throw new InvalidOperationException("Sua conta não tem permissão para esta operação.");

            string mensagem = "Não foi possível concluir a operação. Atualize a agenda e tente novamente.";
            try
            {
                var erro = await resposta.Content.ReadFromJsonAsync<ErroApi>(cancellationToken);
                if (!string.IsNullOrWhiteSpace(erro?.Mensagem))
                    mensagem = erro.Mensagem;
            }
            catch (JsonException) { }
            throw new InvalidOperationException(mensagem);
        }
    }

    public static async Task<IReadOnlyList<HorarioMedicoApi>> ObterAgendaAsync(
        DateTime data, CancellationToken cancellationToken = default)
    {
        // O servidor identifica o médico pelo token, não pelo ID do usuário da interface.
        using var pedido = new HttpRequestMessage(HttpMethod.Get,
            $"api/agenda?data={data:yyyy-MM-dd}");
        using var resposta = await EnviarAsync(pedido, cancellationToken);
        var agenda = await resposta.Content.ReadFromJsonAsync<AgendaDiaApi>(cancellationToken)
            ?? throw new InvalidOperationException("A API retornou uma agenda inválida.");
        return agenda.Medicos.SelectMany(m => m.Horarios).OrderBy(h => h.Inicio).ToList();
    }

    public static async Task<List<PacienteMedicoResposta>> ObterPacientesAsync(CancellationToken ct = default)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, "api/medico/pacientes");
        using var resposta = await EnviarAsync(pedido, ct);
        return await resposta.Content.ReadFromJsonAsync<List<PacienteMedicoResposta>>(ct)
            ?? throw new InvalidOperationException("A API retornou uma lista inválida.");
    }

    public static async Task<List<ProntuarioMedicoResposta>> ObterHistoricoAsync(int idPaciente, CancellationToken ct = default)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, $"api/pacientes/{idPaciente}/prontuarios");
        using var resposta = await EnviarAsync(pedido, ct);
        return await resposta.Content.ReadFromJsonAsync<List<ProntuarioMedicoResposta>>(ct)
            ?? throw new InvalidOperationException("A API retornou um histórico inválido.");
    }

    public static async Task<ProntuarioMedicoResposta> ObterProntuarioAsync(int idConsulta, CancellationToken ct = default)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, $"api/consultas/{idConsulta}/prontuario");
        using var resposta = await EnviarAsync(pedido, ct);
        return await resposta.Content.ReadFromJsonAsync<ProntuarioMedicoResposta>(ct)
            ?? throw new InvalidOperationException("A API retornou um prontuário inválido.");
    }

    public static async Task RegistrarAtendimentoAsync(int idConsulta, string observacoes)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Post,
            $"api/consultas/{idConsulta}/prontuario")
        {
            Content = JsonContent.Create(new { observacoes })
        };
        // Esta operação já grava o prontuário e marca a consulta como REALIZADA na API.
        using var resposta = await EnviarAsync(pedido);
    }
}

internal sealed record ErroApi(string Mensagem);
internal sealed record AgendaDiaApi(string Data, List<AgendaMedicoApi> Medicos);
internal sealed record AgendaMedicoApi(int Id, string Nome, string Especialidade,
    List<HorarioMedicoApi> Horarios);
public sealed record HorarioMedicoApi(int Id, string Inicio, string Fim, string Situacao,
    ConsultaMedicoApi? Consulta);
public sealed record ConsultaMedicoApi(int Id, string Status, string TipoAtendimento,
    PacienteAgendaApi Paciente, ResponsavelAgendaApi Responsavel);
public sealed record PacienteAgendaApi(int Id, string Nome, int Idade);
public sealed record ResponsavelAgendaApi(string Nome, string Telefone);
public sealed record AtendimentoMedicoApi(DateTime Data, HorarioMedicoApi Horario);

public sealed record PacienteMedicoResposta(int Id, string Nome, DateOnly DataNascimento, string Responsavel);
public sealed record ProntuarioMedicoResposta(int Id, int IdConsulta, int IdPaciente, string Observacoes, DateTime DataRegistro)
{
    public string ConsultaTexto => $"Consulta nº {IdConsulta}";
    public string RegistroTexto => $"Registrado em {DataRegistro.ToLocalTime():dd/MM/yyyy HH:mm}";
}

