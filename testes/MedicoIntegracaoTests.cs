using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MySqlConnector;
using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Views.Usuarios.Medico;
using Xunit;

namespace PediAgenda.Testes;

[Collection("MySQL")]
public class MedicoIntegracaoTests
{
    private sealed class RelogioFixo(DateTime local) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(local, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
    private readonly BancoFixture banco;
    public MedicoIntegracaoTests(BancoFixture banco) { this.banco = banco; banco.Reiniciar(); }
    private object Periodo(int dias, string inicio, string fim, int? idMedico = null, int? diaFinal = null) => new
    {
        idMedico,
        dataInicial = banco.Hoje.AddDays(dias).ToString("yyyy-MM-dd"),
        dataFinal = banco.Hoje.AddDays(diaFinal ?? dias).ToString("yyyy-MM-dd"),
        horarioInicial = inicio, horarioFinal = fim
    };

    [Fact]
    public async Task Atendimento_persiste_atualiza_agenda_historico_e_relatorio()
    {
        using var medico = await banco.Autenticado(1);
        var pacientes = await medico.GetFromJsonAsync<JsonElement>("/api/medico/pacientes");
        Assert.Single(pacientes.EnumerateArray());
        var salvo = await medico.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = "Evolução clínica áéç" });
        Assert.Equal(HttpStatusCode.Created, salvo.StatusCode);
        var registro = await medico.GetFromJsonAsync<JsonElement>("/api/consultas/1/prontuario");
        Assert.Equal("Evolução clínica áéç", registro.GetProperty("observacoes").GetString());
        Assert.Equal("REALIZADA", banco.Escalar("SELECT status FROM consulta WHERE id_consulta=1"));
        var historico = await medico.GetFromJsonAsync<JsonElement>("/api/pacientes/1/prontuarios");
        Assert.Single(historico.EnumerateArray());
        var agenda = await medico.GetFromJsonAsync<JsonElement>("/api/agenda?data=" + banco.Hoje.AddDays(-1).ToString("yyyy-MM-dd"));
        Assert.Contains(agenda.GetProperty("medicos")[0].GetProperty("horarios").EnumerateArray(),
            h => h.GetProperty("consulta").ValueKind == JsonValueKind.Object
                && h.GetProperty("consulta").GetProperty("status").GetString() == "REALIZADA");

        using var outroAcesso = await banco.Autenticado(1);
        var persistido = await outroAcesso.GetFromJsonAsync<JsonElement>("/api/consultas/1/prontuario");
        Assert.Equal(registro.GetProperty("id").GetInt32(), persistido.GetProperty("id").GetInt32());

        var relatorio = await medico.PostAsJsonAsync("/api/relatorios", new
        {
            inicio = banco.Hoje.AddDays(-1).ToString("yyyy-MM-dd"),
            fim = banco.Hoje.AddDays(1).ToString("yyyy-MM-dd"),
            idMedico = 2 // Ignorado para médico: usa o token.
        });
        Assert.Equal(HttpStatusCode.Created, relatorio.StatusCode);
        var json = await relatorio.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, json.GetProperty("medico").GetProperty("id").GetInt32());
        Assert.Single(json.GetProperty("consultas").EnumerateArray());
        Assert.Equal(1, json.GetProperty("resumo").GetProperty("totalConsultas").GetInt32());
        var reaberto = await medico.GetFromJsonAsync<JsonElement>("/api/relatorios/" + json.GetProperty("id").GetInt32());
        Assert.Single(reaberto.GetProperty("consultas").EnumerateArray());
        var dto = JsonSerializer.Deserialize<RelatorioMedicoResposta>(json.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        using var excel = ExportadorExcelMedico.Gerar(RelatorioMedicoDocumento.Criar(dto, dto.Inicio, dto.Fim));
        Assert.True(excel.Length > 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Agendada_e_confirmada_aceitam_prontuario_apos_inicio(int consulta)
    {
        using var medico = await banco.Autenticado(1);
        var resposta = await medico.PostAsJsonAsync($"/api/consultas/{consulta}/prontuario", new { observacoes = "Atendimento" });
        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);
    }

    [Fact]
    public async Task Prontuario_recusa_futuro_cancelada_vazio_excesso_e_duplicidade()
    {
        using var medico = await banco.Autenticado(1);
        Assert.Equal(HttpStatusCode.Conflict, (await medico.PostAsJsonAsync("/api/consultas/10/prontuario", new { observacoes = "Futuro" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await medico.PostAsJsonAsync("/api/consultas/4/prontuario", new { observacoes = "Cancelada" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await medico.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = "  " })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await medico.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = new string('á', 33000) })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await medico.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = "Primeiro" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await medico.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = "Segundo" })).StatusCode);
        Assert.Equal(1L, Convert.ToInt64(banco.Escalar("SELECT COUNT(*) FROM prontuario")));
    }

    [Fact]
    public async Task Realizada_sem_registro_pode_receber_prontuario_uma_unica_vez()
    {
        banco.Executar("UPDATE consulta SET status='REALIZADA' WHERE id_consulta=1");
        using var medico = await banco.Autenticado(1);
        Assert.Equal(HttpStatusCode.Created, (await medico.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = "Registro" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await medico.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = "Repetido" })).StatusCode);
    }

    [Fact]
    public async Task Medico_nao_acessa_prontuario_paciente_ou_relatorio_de_outro()
    {
        using var primeiro = await banco.Autenticado(1);
        using var segundo = await banco.Autenticado(2);
        var relatorio = await primeiro.PostAsJsonAsync("/api/relatorios", new { inicio = banco.Hoje.ToString("yyyy-MM-dd"), fim = banco.Hoje.ToString("yyyy-MM-dd") });
        var json = await relatorio.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Forbidden, (await segundo.GetAsync("/api/relatorios/" + json.GetProperty("id").GetInt32())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await segundo.GetAsync("/api/pacientes/1/prontuarios")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await segundo.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = "Outro" })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await primeiro.PostAsJsonAsync("/api/consultas/1/prontuario", new { observacoes = "Dono" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await segundo.GetAsync("/api/consultas/1/prontuario")).StatusCode);
    }

    [Fact]
    public async Task Sem_token_e_perfil_responsavel_nao_acessam_rotas_medicas()
    {
        using var anonimo = banco.Cliente();
        using var responsavel = await banco.Autenticado(3);
        foreach (string rota in new[] { "/api/medico/pacientes", "/api/pacientes/1/prontuarios", "/api/consultas/1/prontuario", "/api/agenda/periodos/bloqueados" })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(rota)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await responsavel.GetAsync(rota)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await responsavel.PostAsJsonAsync("/api/relatorios", new { inicio = "2026-10-01", fim = "2026-10-31" })).StatusCode);
    }

    [Fact]
    public async Task Bloqueio_parcial_cancela_consultas_e_liberacao_nao_restaura()
    {
        using var medico = await banco.Autenticado(1);
        var resposta = await medico.PostAsJsonAsync("/api/agenda/periodos/bloquear", Periodo(1, "10:15", "10:45", idMedico: 2));
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var resultado = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, resultado.GetProperty("horariosAlterados").GetInt32());
        Assert.Equal(2, resultado.GetProperty("consultasCanceladas").GetInt32());
        Assert.Equal(0L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=10")));
        Assert.Equal(1L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=20")));
        var bloqueados = await medico.GetFromJsonAsync<JsonElement>("/api/agenda/periodos/bloqueados");
        Assert.Single(bloqueados.EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, (await medico.PostAsJsonAsync("/api/agenda/periodos/liberar", Periodo(1, "10:00", "11:00"))).StatusCode);
        Assert.Equal(2L, Convert.ToInt64(banco.Escalar("SELECT COUNT(*) FROM consulta WHERE id_consulta IN (10,11) AND status='CANCELADA'")));
        Assert.Equal(2L, Convert.ToInt64(banco.Escalar("SELECT COUNT(*) FROM horario WHERE id_horario IN (10,11) AND disponivel=TRUE")));
    }

    [Fact]
    public async Task Bloqueio_respeita_fronteira_e_periodo_entre_dias()
    {
        using var medico = await banco.Autenticado(1);
        Assert.Equal(HttpStatusCode.OK, (await medico.PostAsJsonAsync("/api/agenda/periodos/bloquear", Periodo(1, "11:00", "09:30", diaFinal: 2))).StatusCode);
        Assert.Equal(1L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=11"))); // Termina às 11, não sobrepõe.
        Assert.Equal(0L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=12")));
        Assert.Equal(0L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=13")));
    }

    [Fact]
    public async Task Bloqueio_com_realizada_faz_rollback_e_liberacao_recusa_consulta_ativa()
    {
        banco.Executar("UPDATE consulta SET status='REALIZADA' WHERE id_consulta=11");
        using var medico = await banco.Autenticado(1);
        Assert.Equal(HttpStatusCode.Conflict, (await medico.PostAsJsonAsync("/api/agenda/periodos/bloquear", Periodo(1, "10:00", "11:00"))).StatusCode);
        Assert.Equal("AGENDADA", banco.Escalar("SELECT status FROM consulta WHERE id_consulta=10"));
        Assert.Equal(2L, Convert.ToInt64(banco.Escalar("SELECT COUNT(*) FROM horario WHERE id_horario IN (10,11) AND disponivel=TRUE")));
        banco.Executar("UPDATE horario SET disponivel=FALSE WHERE id_horario=10");
        Assert.Equal(HttpStatusCode.Conflict, (await medico.PostAsJsonAsync("/api/agenda/periodos/liberar", Periodo(1, "10:00", "10:30"))).StatusCode);
        Assert.Equal(0L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=10")));
    }

    [Fact]
    public async Task Recepcao_continua_usando_o_mesmo_bloqueio_e_medico_nao_troca_dono()
    {
        using var recepcao = await banco.Autenticado(4);
        Assert.Equal(HttpStatusCode.OK, (await recepcao.PostAsJsonAsync("/api/agenda/periodos/bloquear", Periodo(1, "10:00", "10:30", 2))).StatusCode);
        Assert.Equal(0L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=20")));
        using var medico = await banco.Autenticado(1);
        var agenda = await medico.GetFromJsonAsync<JsonElement>("/api/agenda?medico=2&data=" + banco.Hoje.AddDays(1).ToString("yyyy-MM-dd"));
        Assert.Equal(1, agenda.GetProperty("medicos")[0].GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task Relatorio_vazio_e_periodos_invalidos()
    {
        using var medico = await banco.Autenticado(1);
        var vazio = await medico.PostAsJsonAsync("/api/relatorios", new { inicio = banco.Hoje.ToString("yyyy-MM-dd"), fim = banco.Hoje.ToString("yyyy-MM-dd") });
        var json = await vazio.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.Created, vazio.StatusCode);
        Assert.Empty(json.GetProperty("consultas").EnumerateArray());
        Assert.Equal(0, json.GetProperty("resumo").GetProperty("totalConsultas").GetInt32());
        foreach (var periodo in new[] { new { inicio = "2026-10-28", fim = "2026-10-01" }, new { inicio = "2020-01-01", fim = "2026-10-28" } })
            Assert.Equal(HttpStatusCode.BadRequest, (await medico.PostAsJsonAsync("/api/relatorios", periodo)).StatusCode);
    }

    [Fact]
    public async Task Cinco_senhas_curtas_bloqueiam_pela_api_e_sao_auditadas()
    {
        using var client = banco.Cliente();
        for (int i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login", new { email = "usuario1@teste.local", senha = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Locked, (await client.PostAsJsonAsync("/api/auth/login", new { email = "usuario1@teste.local", senha = "x" })).StatusCode);
        Assert.Equal(HttpStatusCode.Locked, (await client.PostAsJsonAsync("/api/auth/login", new { email = "usuario1@teste.local", senha = "senhaDemo123" })).StatusCode);
        Assert.Equal(6L, Convert.ToInt64(banco.Escalar("SELECT COUNT(*) FROM tentativa_login WHERE id_usuario=1")));
    }

    [Fact]
    public void Periodo_futuro_que_atinge_horario_em_andamento_e_recusado_sem_alteracoes()
    {
        var dia = banco.Hoje.AddDays(1);
        var repo = new BloqueioPeriodoRepositorioMySql(banco.Conexao,
            new RelogioFixo(dia.ToDateTime(new TimeOnly(10, 10))));
        Assert.Throws<PeriodoAgendaException>(() => repo.Bloquear(1, dia, dia,
            new TimeOnly(10, 15), new TimeOnly(10, 45)));
        Assert.Equal("AGENDADA", banco.Escalar("SELECT status FROM consulta WHERE id_consulta=10"));
        Assert.Equal(1L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=10")));
    }

    [Fact]
    public async Task Token_invalido_e_conta_inativa_sao_recusados()
    {
        using var cliente = banco.Cliente();
        cliente.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalido");
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/medico/pacientes")).StatusCode);
        banco.Executar("UPDATE usuario SET ativo=FALSE WHERE id_usuario=1");
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/auth/login", new { email = "usuario1@teste.local", senha = "senhaDemo123" })).StatusCode);
    }

    [Fact]
    public async Task Agendamento_concorrente_com_bloqueio_nao_deixa_consulta_ativa_em_horario_bloqueado()
    {
        var dia = banco.Hoje.AddDays(1);
        var inicio = new TimeOnly(14, 0); var fim = new TimeOnly(14, 30);
        var bloqueio = Task.Run(() => new BloqueioPeriodoRepositorioMySql(banco.Conexao).Bloquear(1, dia, dia, inicio, fim));
        var reserva = Task.Run(() => new ConsultaRepositorioMySql(banco.Conexao).Reservar(1, 1, 14, "PARTICULAR", dia.AddDays(-1).ToDateTime(TimeOnly.MinValue)));
        await Task.WhenAll(bloqueio, reserva);
        Assert.Equal(0L, Convert.ToInt64(banco.Escalar("SELECT COUNT(*) FROM consulta c JOIN horario h ON h.id_horario=c.id_horario WHERE h.id_horario=14 AND h.disponivel=FALSE AND c.status<>'CANCELADA'")));
        Assert.Equal(0L, Convert.ToInt64(banco.Escalar("SELECT disponivel FROM horario WHERE id_horario=14")));
    }
}
