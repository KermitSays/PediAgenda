using MySqlConnector;
using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public class HorarioRepositorioMySql(string stringDeConexao) : IHorarioRepositorio
{
    private readonly string _conexao = stringDeConexao;

    public IReadOnlyList<HorarioLivre> ListarLivres(DateOnly data, TimeOnly aPartirDe,
                                                    string? especialidade, int? idMedico)
    {
        // Livre = não bloqueado (disponivel) E sem consulta ativa. A consulta
        // cancelada devolve o horário para a agenda. O índice ix_consulta_horario
        // (id_horario, status) foi criado no modelo justamente para este NOT EXISTS.
        const string sql = """
            SELECT   h.id_horario, h.data_horario, h.hora_inicio, h.hora_fim,
                     m.id_medico, u.nome, m.especialidade
            FROM     horario       h
            JOIN     agenda_medica a ON a.id_agenda  = h.id_agenda
            JOIN     medico        m ON m.id_medico  = a.id_medico
            JOIN     usuario       u ON u.id_usuario = m.id_usuario
            WHERE    h.data_horario = @data
              AND    h.hora_inicio >= @aPartirDe
              AND    h.disponivel   = TRUE
              AND    u.ativo        = TRUE
              AND    (@especialidade IS NULL OR m.especialidade = @especialidade)
              AND    (@medico        IS NULL OR m.id_medico     = @medico)
              AND    NOT EXISTS (SELECT 1 FROM consulta c
                                 WHERE  c.id_horario = h.id_horario
                                   AND  c.status <> 'CANCELADA')
            ORDER BY u.nome, h.hora_inicio
            """;

        using var con = new MySqlConnection(_conexao);
        con.Open();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@data", data);
        cmd.Parameters.AddWithValue("@aPartirDe", aPartirDe.ToTimeSpan());
        cmd.Parameters.AddWithValue("@especialidade", (object?)especialidade ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@medico", (object?)idMedico ?? DBNull.Value);

        var livres = new List<HorarioLivre>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            livres.Add(new HorarioLivre(
                r.GetInt32("id_horario"),
                r.GetDateOnly("data_horario"),
                TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio")),
                TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_fim")),
                r.GetInt32("id_medico"),
                r.GetString("nome"),
                r.GetString("especialidade")));
        }
        return livres;
    }
}
