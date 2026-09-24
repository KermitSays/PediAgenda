using MySqlConnector;
using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public class AgendaRepositorioMySql(string stringDeConexao) : IAgendaRepositorio
{
    private readonly string _conexao = stringDeConexao;

    private MySqlConnection Abrir()
    {
        var con = new MySqlConnection(_conexao);
        con.Open();
        return con;
    }

    public int? IdMedicoDoUsuario(int idUsuario) =>
        Escalar("SELECT id_medico FROM medico WHERE id_usuario = @id", idUsuario);

    public int? IdAgendaDoMedico(int idMedico) =>
        Escalar("SELECT id_agenda FROM agenda_medica WHERE id_medico = @id", idMedico);

    private int? Escalar(string sql, int id)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteScalar() is { } valor and not DBNull ? Convert.ToInt32(valor) : null;
    }

    public int AbrirHorarios(int idAgenda, DateOnly data, IReadOnlyList<(TimeOnly Inicio, TimeOnly Fim)> blocos)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();

        var existentes = new List<(TimeOnly Inicio, TimeOnly Fim)>();
        using (var cmd = new MySqlCommand(
            "SELECT hora_inicio, hora_fim FROM horario WHERE id_agenda = @agenda AND data_horario = @data FOR UPDATE",
            con, tx))
        {
            cmd.Parameters.AddWithValue("@agenda", idAgenda);
            cmd.Parameters.AddWithValue("@data", data);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                existentes.Add((TimeOnly.FromTimeSpan(r.GetTimeSpan(0)), TimeOnly.FromTimeSpan(r.GetTimeSpan(1))));
        }

        var criados = 0;
        foreach (var (inicio, fim) in blocos)
        {
            // Pula o bloco que encosta em outro já existente: 08:20–08:40 não
            // pode entrar numa agenda que já tem 08:00–08:30.
            if (existentes.Any(e => inicio < e.Fim && fim > e.Inicio))
                continue;

            using var cmd = new MySqlCommand("""
                INSERT INTO horario (data_horario, hora_inicio, hora_fim, id_agenda)
                VALUES (@data, @inicio, @fim, @agenda)
                """, con, tx);
            cmd.Parameters.AddWithValue("@data", data);
            cmd.Parameters.AddWithValue("@inicio", inicio.ToTimeSpan());
            cmd.Parameters.AddWithValue("@fim", fim.ToTimeSpan());
            cmd.Parameters.AddWithValue("@agenda", idAgenda);
            cmd.ExecuteNonQuery();
            existentes.Add((inicio, fim));
            criados++;
        }

        tx.Commit();
        return criados;
    }

    public IReadOnlyList<ItemAgenda> AgendaDoDia(DateOnly data, int? idMedico)
    {
        // Cada horário aparece uma vez: a reserva garante no máximo uma
        // consulta ativa por horário, e as canceladas ficam de fora do JOIN.
        const string sql = """
            SELECT    h.id_horario, h.data_horario, h.hora_inicio, h.hora_fim, h.disponivel,
                      m.id_medico, um.nome AS nome_medico, m.especialidade,
                      c.id_consulta, c.status, c.tipo_atendimento,
                      p.id_paciente, p.nome AS nome_paciente, p.data_nascimento,
                      ur.nome AS nome_responsavel, r.telefone
            FROM      horario       h
            JOIN      agenda_medica a  ON a.id_agenda      = h.id_agenda
            JOIN      medico        m  ON m.id_medico      = a.id_medico
            JOIN      usuario       um ON um.id_usuario    = m.id_usuario
            LEFT JOIN consulta      c  ON c.id_horario     = h.id_horario AND c.status <> 'CANCELADA'
            LEFT JOIN paciente      p  ON p.id_paciente    = c.id_paciente
            LEFT JOIN responsavel   r  ON r.id_responsavel = p.id_responsavel
            LEFT JOIN usuario       ur ON ur.id_usuario    = r.id_usuario
            WHERE     h.data_horario = @data
              AND     (@medico IS NULL OR m.id_medico = @medico)
            ORDER BY  um.nome, h.hora_inicio
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@data", data);
        cmd.Parameters.AddWithValue("@medico", (object?)idMedico ?? DBNull.Value);

        var itens = new List<ItemAgenda>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            var livre = r.IsDBNull(r.GetOrdinal("id_consulta"));
            itens.Add(new ItemAgenda(
                r.GetInt32("id_horario"),
                r.GetDateOnly("data_horario"),
                TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio")),
                TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_fim")),
                r.GetBoolean("disponivel"),
                r.GetInt32("id_medico"),
                r.GetString("nome_medico"),
                r.GetString("especialidade"),
                livre ? null : r.GetInt32("id_consulta"),
                livre ? null : r.GetString("status"),
                livre ? null : r.GetString("tipo_atendimento"),
                livre ? null : r.GetInt32("id_paciente"),
                livre ? null : r.GetString("nome_paciente"),
                livre ? null : r.GetDateOnly("data_nascimento"),
                livre ? null : r.GetString("nome_responsavel"),
                livre ? null : r.GetString("telefone")));
        }
        return itens;
    }

    public ResultadoBloqueio AlterarDisponibilidade(int idHorario, int? somenteDoMedico, bool disponivel)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();

        // Trava o horário: a reserva trava o mesmo registro, então bloquear e
        // agendar o mesmo horário ao mesmo tempo acontecem um depois do outro.
        using (var cmd = new MySqlCommand("""
            SELECT a.id_medico
            FROM   horario       h
            JOIN   agenda_medica a ON a.id_agenda = h.id_agenda
            WHERE  h.id_horario = @horario
            FOR UPDATE
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@horario", idHorario);
            var dono = cmd.ExecuteScalar();
            if (dono is null or DBNull)
                return ResultadoBloqueio.NaoEncontrado;
            if (somenteDoMedico is not null && Convert.ToInt32(dono) != somenteDoMedico)
                return ResultadoBloqueio.DeOutroMedico;
        }

        // Leitura com trava, para enxergar uma consulta gravada agora há pouco.
        if (!disponivel)
        {
            using var cmd = new MySqlCommand("""
                SELECT COUNT(*) FROM consulta
                WHERE  id_horario = @horario AND status <> 'CANCELADA'
                FOR UPDATE
                """, con, tx);
            cmd.Parameters.AddWithValue("@horario", idHorario);
            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                return ResultadoBloqueio.TemConsulta;
        }

        using (var cmd = new MySqlCommand(
            "UPDATE horario SET disponivel = @disponivel WHERE id_horario = @horario", con, tx))
        {
            cmd.Parameters.AddWithValue("@disponivel", disponivel);
            cmd.Parameters.AddWithValue("@horario", idHorario);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return ResultadoBloqueio.Alterado;
    }

    public ResultadoTransicao MudarStatusConsulta(int idConsulta, int? somenteDoMedico,
                                                 IReadOnlySet<string> statusPermitidos, string novoStatus,
                                                 DateTime agora, bool exigeJaTerComecado)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();

        using (var cmd = new MySqlCommand("""
            SELECT c.status, h.data_horario, h.hora_inicio, a.id_medico
            FROM   consulta      c
            JOIN   horario       h ON h.id_horario = c.id_horario
            JOIN   agenda_medica a ON a.id_agenda  = h.id_agenda
            WHERE  c.id_consulta = @consulta
            FOR UPDATE
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@consulta", idConsulta);
            using var r = cmd.ExecuteReader();
            if (!r.Read())
                return ResultadoTransicao.NaoEncontrada;
            if (somenteDoMedico is not null && r.GetInt32("id_medico") != somenteDoMedico)
                return ResultadoTransicao.DeOutroMedico;
            if (!statusPermitidos.Contains(r.GetString("status")))
                return ResultadoTransicao.StatusNaoPermite;

            var inicio = r.GetDateOnly("data_horario")
                          .ToDateTime(TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio")));
            if (exigeJaTerComecado && inicio > agora)
                return ResultadoTransicao.AindaNaoComecou;
            if (!exigeJaTerComecado && inicio <= agora)
                return ResultadoTransicao.JaComecou;
        }

        using (var cmd = new MySqlCommand(
            "UPDATE consulta SET status = @status WHERE id_consulta = @consulta", con, tx))
        {
            cmd.Parameters.AddWithValue("@status", novoStatus);
            cmd.Parameters.AddWithValue("@consulta", idConsulta);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return ResultadoTransicao.Alterada;
    }
}
