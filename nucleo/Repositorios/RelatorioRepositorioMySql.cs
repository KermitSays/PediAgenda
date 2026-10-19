using MySqlConnector;
using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public class RelatorioRepositorioMySql(string stringDeConexao) : IRelatorioRepositorio
{
    private readonly string _conexao = stringDeConexao;

    private MySqlConnection Abrir()
    {
        var con = new MySqlConnection(_conexao);
        con.Open();
        return con;
    }

    public int Registrar(int idMedico, DateOnly inicio, DateOnly fim)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand("""
            INSERT INTO relatorio_atendimento (inicio_periodo, fim_periodo, id_medico)
            VALUES (@inicio, @fim, @medico)
            """, con);
        cmd.Parameters.AddWithValue("@inicio", inicio);
        cmd.Parameters.AddWithValue("@fim", fim);
        cmd.Parameters.AddWithValue("@medico", idMedico);
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    private const string SelectRelatorio = """
        SELECT   r.id_relatorio, r.id_medico, u.nome, m.especialidade,
                 r.inicio_periodo, r.fim_periodo, r.gerado_em
        FROM     relatorio_atendimento r
        JOIN     medico  m ON m.id_medico  = r.id_medico
        JOIN     usuario u ON u.id_usuario = m.id_usuario
        """;

    public RelatorioGerado? Buscar(int idRelatorio)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(SelectRelatorio + " WHERE r.id_relatorio = @id", con);
        cmd.Parameters.AddWithValue("@id", idRelatorio);
        using var r = cmd.ExecuteReader();
        return r.Read() ? LerRelatorio(r) : null;
    }

    public IReadOnlyList<RelatorioGerado> Listar(int? idMedico)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(SelectRelatorio + """

            WHERE    (@medico IS NULL OR r.id_medico = @medico)
            ORDER BY r.gerado_em DESC, r.id_relatorio DESC
            """, con);
        cmd.Parameters.AddWithValue("@medico", (object?)idMedico ?? DBNull.Value);

        var lista = new List<RelatorioGerado>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            lista.Add(LerRelatorio(r));
        return lista;
    }

    public IReadOnlyList<LinhaRelatorio> Consultas(int idMedico, DateOnly inicio, DateOnly fim)
    {
        // Filtra por agenda e data: o índice único de horario
        // (id_agenda, data_horario, hora_inicio) já cobre essa busca.
        const string sql = """
            SELECT   h.data_horario, h.hora_inicio, p.nome, p.data_nascimento,
                     c.tipo_atendimento, c.status
            FROM     consulta      c
            JOIN     horario       h ON h.id_horario  = c.id_horario
            JOIN     agenda_medica a ON a.id_agenda   = h.id_agenda
            JOIN     paciente      p ON p.id_paciente = c.id_paciente
            WHERE    a.id_medico = @medico
              AND    h.data_horario BETWEEN @inicio AND @fim
            ORDER BY h.data_horario, h.hora_inicio
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@medico", idMedico);
        cmd.Parameters.AddWithValue("@inicio", inicio);
        cmd.Parameters.AddWithValue("@fim", fim);

        var linhas = new List<LinhaRelatorio>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            linhas.Add(new LinhaRelatorio(
                r.GetDateOnly("data_horario"),
                TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio")),
                r.GetString("nome"),
                r.GetDateOnly("data_nascimento"),
                r.GetString("tipo_atendimento"),
                r.GetString("status")));
        }
        return linhas;
    }

    public (int Ofertados, int Bloqueados) Horarios(int idMedico, DateOnly inicio, DateOnly fim)
    {
        const string sql = """
            SELECT COUNT(*), COALESCE(SUM(h.disponivel = FALSE), 0)
            FROM   horario       h
            JOIN   agenda_medica a ON a.id_agenda = h.id_agenda
            WHERE  a.id_medico = @medico
              AND  h.data_horario BETWEEN @inicio AND @fim
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@medico", idMedico);
        cmd.Parameters.AddWithValue("@inicio", inicio);
        cmd.Parameters.AddWithValue("@fim", fim);
        using var r = cmd.ExecuteReader();
        r.Read();
        return (Convert.ToInt32(r.GetValue(0)), Convert.ToInt32(r.GetValue(1)));
    }

    private static RelatorioGerado LerRelatorio(MySqlDataReader r) => new(
        r.GetInt32("id_relatorio"),
        r.GetInt32("id_medico"),
        r.GetString("nome"),
        r.GetString("especialidade"),
        r.GetDateOnly("inicio_periodo"),
        r.GetDateOnly("fim_periodo"),
        r.GetDateTime("gerado_em"));
}
