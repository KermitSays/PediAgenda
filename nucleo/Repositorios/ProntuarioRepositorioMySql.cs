using MySqlConnector;
using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public class ProntuarioRepositorioMySql(string conexao) : IProntuarioRepositorio
{
    private MySqlConnection Abrir()
    {
        var con = new MySqlConnection(conexao);
        con.Open();
        return con;
    }

    public IReadOnlyList<PacienteDoMedico> Pacientes(int idMedico)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand("""
            SELECT DISTINCT p.id_paciente, p.nome, p.data_nascimento, u.nome AS responsavel
            FROM paciente p
            JOIN responsavel r ON r.id_responsavel = p.id_responsavel
            JOIN usuario u ON u.id_usuario = r.id_usuario
            JOIN consulta c ON c.id_paciente = p.id_paciente
            JOIN horario h ON h.id_horario = c.id_horario
            JOIN agenda_medica a ON a.id_agenda = h.id_agenda
            WHERE a.id_medico = @medico AND c.status <> 'CANCELADA'
            ORDER BY p.nome, p.id_paciente
            """, con);
        cmd.Parameters.AddWithValue("@medico", idMedico);
        using var r = cmd.ExecuteReader();
        var lista = new List<PacienteDoMedico>();
        while (r.Read()) lista.Add(new(r.GetInt32(0), r.GetString(1), r.GetDateOnly(2), r.GetString(3)));
        return lista;
    }

    public bool TemPaciente(int idMedico, int idPaciente)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand("""
            SELECT EXISTS (
                SELECT 1 FROM consulta c
                JOIN horario h ON h.id_horario = c.id_horario
                JOIN agenda_medica a ON a.id_agenda = h.id_agenda
                WHERE a.id_medico = @medico AND c.id_paciente = @paciente
                  AND c.status <> 'CANCELADA')
            """, con);
        cmd.Parameters.AddWithValue("@medico", idMedico);
        cmd.Parameters.AddWithValue("@paciente", idPaciente);
        return Convert.ToBoolean(cmd.ExecuteScalar());
    }

    private const string SelectRegistro = """
        SELECT p.id_prontuario, c.id_consulta, c.id_paciente, p.observacoes, p.data_registro
        FROM prontuario p
        JOIN consulta c ON c.id_consulta = p.id_consulta
        JOIN horario h ON h.id_horario = c.id_horario
        JOIN agenda_medica a ON a.id_agenda = h.id_agenda
        WHERE a.id_medico = @medico
        """;

    public IReadOnlyList<Prontuario> Historico(int idMedico, int idPaciente)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(SelectRegistro + """

            AND c.id_paciente = @paciente ORDER BY p.data_registro DESC, p.id_prontuario DESC
            """, con);
        cmd.Parameters.AddWithValue("@medico", idMedico);
        cmd.Parameters.AddWithValue("@paciente", idPaciente);
        using var r = cmd.ExecuteReader();
        var lista = new List<Prontuario>();
        while (r.Read()) lista.Add(Ler(r));
        return lista;
    }

    public Prontuario? Buscar(int idMedico, int idConsulta)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(SelectRegistro + " AND c.id_consulta = @consulta", con);
        cmd.Parameters.AddWithValue("@medico", idMedico);
        cmd.Parameters.AddWithValue("@consulta", idConsulta);
        using var r = cmd.ExecuteReader();
        return r.Read() ? Ler(r) : null;
    }

    public ResultadoRegistroProntuario Registrar(int idMedico, int idConsulta,
        string observacoes, DateTime agoraClinica, DateTime agoraUtc)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();
        // A consulta fica travada até gravar o prontuário e concluir o atendimento.
        using (var cmd = new MySqlCommand("""
            SELECT c.status, h.data_horario, h.hora_inicio
            FROM consulta c
            JOIN horario h ON h.id_horario = c.id_horario
            JOIN agenda_medica a ON a.id_agenda = h.id_agenda
            WHERE c.id_consulta = @consulta AND a.id_medico = @medico FOR UPDATE
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@consulta", idConsulta);
            cmd.Parameters.AddWithValue("@medico", idMedico);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return ResultadoRegistroProntuario.NaoEncontrada;
            if (r.GetString("status") == StatusConsulta.Cancelada)
                return ResultadoRegistroProntuario.Cancelada;
            if (r.GetDateOnly("data_horario").ToDateTime(
                    TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio"))) > agoraClinica)
                return ResultadoRegistroProntuario.AindaNaoComecou;
        }

        using (var cmd = new MySqlCommand(
            "SELECT id_prontuario FROM prontuario WHERE id_consulta = @id FOR UPDATE", con, tx))
        {
            cmd.Parameters.AddWithValue("@id", idConsulta);
            if (cmd.ExecuteScalar() is not (null or DBNull))
                return ResultadoRegistroProntuario.JaRegistrado;
        }
        using (var cmd = new MySqlCommand("""
            INSERT INTO prontuario (observacoes, data_registro, id_consulta)
            VALUES (@texto, @agora, @id)
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@texto", observacoes);
            cmd.Parameters.AddWithValue("@agora", agoraUtc);
            cmd.Parameters.AddWithValue("@id", idConsulta);
            cmd.ExecuteNonQuery();
        }
        using (var cmd = new MySqlCommand(
            "UPDATE consulta SET status = 'REALIZADA' WHERE id_consulta = @id", con, tx))
        {
            cmd.Parameters.AddWithValue("@id", idConsulta);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
        return ResultadoRegistroProntuario.Registrado;
    }

    private static Prontuario Ler(MySqlDataReader r) =>
        new(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetString(3), DateTime.SpecifyKind(r.GetDateTime(4), DateTimeKind.Utc));
}
