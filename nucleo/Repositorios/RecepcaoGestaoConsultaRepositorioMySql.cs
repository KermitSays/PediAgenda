using MySqlConnector;

namespace PediAgenda.Nucleo.Repositorios;

public sealed record ConsultaRecepcaoDetalhe(
    int Id, string Paciente, string Responsavel, string Medico,
    DateOnly Data, TimeOnly Inicio, string Status);

public enum ResultadoCancelarConsultaRecepcao
{
    Cancelada,
    NaoEncontrada,
    JaEncerrada,
    JaComecou
}

/// <summary>
/// Opera somente dados administrativos. A recepção não acessa prontuários.
/// Não altera a estrutura do banco. O cancelamento é lógico (status CANCELADA).
/// </summary>
public sealed class RecepcaoGestaoConsultaRepositorioMySql(string stringDeConexao)
{
    private MySqlConnection Abrir()
    {
        var con = new MySqlConnection(stringDeConexao);
        con.Open();
        return con;
    }

    public ConsultaRecepcaoDetalhe? Buscar(int idConsulta)
    {
        const string sql = """
            SELECT c.id_consulta, c.status,
                   h.data_horario, h.hora_inicio,
                   p.nome AS paciente, ur.nome AS responsavel,
                   um.nome AS medico
            FROM consulta c
            JOIN paciente p ON p.id_paciente = c.id_paciente
            JOIN responsavel r ON r.id_responsavel = p.id_responsavel
            JOIN usuario ur ON ur.id_usuario = r.id_usuario
            JOIN horario h ON h.id_horario = c.id_horario
            JOIN agenda_medica a ON a.id_agenda = h.id_agenda
            JOIN medico m ON m.id_medico = a.id_medico
            JOIN usuario um ON um.id_usuario = m.id_usuario
            WHERE c.id_consulta = @consulta
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@consulta", idConsulta);
        using var r = cmd.ExecuteReader();
        return r.Read()
            ? new ConsultaRecepcaoDetalhe(
                r.GetInt32("id_consulta"),
                r.GetString("paciente"),
                r.GetString("responsavel"),
                r.GetString("medico"),
                r.GetDateOnly("data_horario"),
                TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio")),
                r.GetString("status"))
            : null;
    }

    public ResultadoCancelarConsultaRecepcao Cancelar(int idConsulta, DateTime agora)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();

        // Trava a consulta para não concorrer com confirmação ou atendimento.
        using (var cmd = new MySqlCommand("""
            SELECT c.status, h.data_horario, h.hora_inicio
            FROM consulta c
            JOIN horario h ON h.id_horario = c.id_horario
            WHERE c.id_consulta = @consulta
            FOR UPDATE
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@consulta", idConsulta);
            using var r = cmd.ExecuteReader();
            if (!r.Read())
                return ResultadoCancelarConsultaRecepcao.NaoEncontrada;

            string status = r.GetString("status");
            if (status is not ("AGENDADA" or "CONFIRMADA"))
                return ResultadoCancelarConsultaRecepcao.JaEncerrada;

            DateTime inicio = r.GetDateOnly("data_horario").ToDateTime(
                TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio")));

            if (inicio <= agora)
                return ResultadoCancelarConsultaRecepcao.JaComecou;
        }

        using (var cmd = new MySqlCommand("""
            UPDATE consulta
            SET status = 'CANCELADA'
            WHERE id_consulta = @consulta
              AND status IN ('AGENDADA', 'CONFIRMADA')
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@consulta", idConsulta);
            if (cmd.ExecuteNonQuery() != 1)
                return ResultadoCancelarConsultaRecepcao.JaEncerrada;
        }

        tx.Commit();
        // O horário fica reutilizável automaticamente, porque as consultas
        // CANCELADA não entram na busca das consultas ativas do horário.
        return ResultadoCancelarConsultaRecepcao.Cancelada;
    }
}
