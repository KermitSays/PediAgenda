using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PediAgenda.Views.Usuarios.Recepcao;

namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(IdMedico), "IdMedico")]
public partial class BloquearHorario : ContentPage
{
    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    private int? idMedicoSelecionado;
    private string idMedico = string.Empty;
    private bool processando;

    public string IdMedico
    {
        get => idMedico;
        set
        {
            idMedico = value ?? string.Empty;
            idMedicoSelecionado = int.TryParse(value, out int id) && id > 0
                ? id
                : null;
        }
    }

    public BloquearHorario()
    {
        InitializeComponent();
        DataInicialPicker.Date = DateTime.Today;
        DataFinalPicker.Date = DateTime.Today;
        HorarioInicialPicker.Time = new TimeSpan(12, 0, 0);
        HorarioFinalPicker.Time = new TimeSpan(12, 30, 0);
    }

    private async void BloquearHorarioButton_Clicked(object sender, EventArgs e)
    {
        if (processando)
            return;

        DateTime dataInicial = (DataInicialPicker.Date ?? DateTime.Today).Date;
        DateTime dataFinal = (DataFinalPicker.Date ?? DateTime.Today).Date;
        TimeSpan horarioInicial = HorarioInicialPicker.Time ?? new TimeSpan(12, 0, 0);
        TimeSpan horarioFinal = HorarioFinalPicker.Time ?? new TimeSpan(12, 30, 0);

        if (dataFinal < dataInicial)
        {
            await DisplayAlertAsync("Atenção", "A Data Final não pode ser anterior à Data Inicial.", "OK");
            return;
        }

        DateTime inicioPeriodo = dataInicial.Add(horarioInicial);
        DateTime fimPeriodo = dataFinal.Add(horarioFinal);

        if (fimPeriodo <= inicioPeriodo)
        {
            await DisplayAlertAsync("Atenção", "A data e o horário finais devem ser posteriores ao início do bloqueio.", "OK");
            return;
        }

        processando = true;
        try
        {
            // A API respeita o médico autenticado; a Recepção informa o ID real.
            // A consulta prévia identifica as consultas que seriam canceladas.
            var consultasAfetadas = await BuscarConsultasAfetadasAsync(
                dataInicial, dataFinal, horarioInicial, horarioFinal);

            string mensagem =
                $"Deseja bloquear o período?\n\n" +
                $"Período: {dataInicial:dd/MM/yyyy} {horarioInicial:hh\\:mm} " +
                $"até {dataFinal:dd/MM/yyyy} {horarioFinal:hh\\:mm}";

            if (consultasAfetadas.Count > 0)
            {
                mensagem += $"\n\nAtenção: {consultasAfetadas.Count} " +
                            (consultasAfetadas.Count == 1
                                ? "consulta será afetada."
                                : "consultas serão afetadas.");
                mensagem += "\n\nConsultas:";
                foreach (string consulta in consultasAfetadas.Take(12))
                    mensagem += "\n• " + consulta;
                if (consultasAfetadas.Count > 12)
                    mensagem += $"\n• ... e mais {consultasAfetadas.Count - 12} consulta(s).";
                mensagem += "\n\nAs consultas afetadas serão canceladas.";
            }

            bool confirmar = await DisplayAlertAsync(
                "Confirmar bloqueio", mensagem, "Bloquear", "Cancelar");
            if (!confirmar)
                return;

            var token = await ObterTokenAsync();
            using var requisicao = new HttpRequestMessage(
                HttpMethod.Post, "api/agenda/periodos/bloquear")
            {
                Content = JsonContent.Create(new
                {
                    idMedico = idMedicoSelecionado,
                    dataInicial = dataInicial.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    dataFinal = dataFinal.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    horarioInicial = horarioInicial.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
                    horarioFinal = horarioFinal.ToString(@"hh\:mm", CultureInfo.InvariantCulture)
                })
            };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var resposta = await http.SendAsync(requisicao);
            if (!resposta.IsSuccessStatusCode)
                throw new InvalidOperationException(await LerErroAsync(resposta));

            var resultado = await resposta.Content.ReadFromJsonAsync<ResultadoBloqueioPeriodoApi>();
            int horarios = resultado?.HorariosAlterados ?? 0;
            int canceladas = resultado?.ConsultasCanceladas ?? 0;

            await DisplayAlertAsync(
                "Período bloqueado",
                $"Bloqueio registrado no banco de dados.\n" +
                $"Horários bloqueados: {horarios}.\n" +
                $"Consultas canceladas: {canceladas}.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Não foi possível bloquear o período", ex.Message, "OK");
        }
        finally
        {
            processando = false;
        }
    }

    private async Task<List<string>> BuscarConsultasAfetadasAsync(
        DateTime dataInicial, DateTime dataFinal,
        TimeSpan horarioInicial, TimeSpan horarioFinal)
    {
        var consultas = new List<string>();
        var idsEncontrados = new HashSet<int>();
        DateTime inicioPeriodo = dataInicial.Add(horarioInicial);
        DateTime fimPeriodo = dataFinal.Add(horarioFinal);

        for (DateTime dia = dataInicial; dia <= dataFinal; dia = dia.AddDays(1))
        {
            var agenda = await AgendaRecepcaoApi.ConsultarAsync(dia, idMedicoSelecionado);
            foreach (var medico in agenda.Medicos)
            {
                foreach (var horario in medico.Horarios)
                {
                    if (!TimeSpan.TryParseExact(horario.Inicio, @"hh\:mm",
                            CultureInfo.InvariantCulture, out var inicio) ||
                        !TimeSpan.TryParseExact(horario.Fim, @"hh\:mm",
                            CultureInfo.InvariantCulture, out var fim) ||
                        dia.Add(inicio) < inicioPeriodo ||
                        dia.Add(fim) > fimPeriodo)
                        continue;

                    if (horario.Consulta is not { } consulta ||
                        !idsEncontrados.Add(consulta.Id))
                        continue;

                    if (consulta.Status != "AGENDADA" && consulta.Status != "CONFIRMADA")
                    {
                        throw new InvalidOperationException(
                            "O período contém uma consulta que não pode ser cancelada " +
                            "automaticamente. Nenhuma alteração foi realizada.");
                    }

                    consultas.Add($"{dia:dd/MM/yyyy} {horario.Inicio} - " +
                                  (string.IsNullOrWhiteSpace(consulta.Paciente?.Nome)
                                      ? "Paciente não informado"
                                      : consulta.Paciente.Nome));
                }
            }
        }

        return consultas;
    }

    private static async Task<string> ObterTokenAsync()
    {
        string? token = await SecureStorage.Default.GetAsync("pediagenda_token");
        return string.IsNullOrWhiteSpace(token)
            ? throw new InvalidOperationException("É necessário fazer login novamente.")
            : token;
    }

    private static async Task<string> LerErroAsync(HttpResponseMessage resposta)
    {
        string texto = await resposta.Content.ReadAsStringAsync();
        try
        {
            using var json = JsonDocument.Parse(texto);
            if (json.RootElement.TryGetProperty("mensagem", out var mensagem) &&
                mensagem.ValueKind == JsonValueKind.String)
                return mensagem.GetString() ?? "Falha ao bloquear o período.";
        }
        catch (JsonException)
        {
            // Resposta sem JSON: usamos a mensagem de status HTTP.
        }

        return $"A API retornou erro HTTP {(int)resposta.StatusCode}.";
    }

    private async void CancelarButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private sealed class ResultadoBloqueioPeriodoApi
    {
        public int HorariosAlterados { get; set; }
        public int ConsultasCanceladas { get; set; }
    }
}
