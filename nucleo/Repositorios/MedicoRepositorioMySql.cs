using MySqlConnector;

namespace PediAgenda.Nucleo.Repositorios;

public class MedicoRepositorioMySql(string stringDeConexao)
{
    private readonly string _conexao = stringDeConexao;

    public List<MedicoListaDto> Listar(string? especialidade)
    {
        const string sql = """
            SELECT
                m.id_medico,
                u.nome,
                m.especialidade
            FROM medico m
            INNER JOIN usuario u
                ON m.id_usuario = u.id_usuario
            WHERE u.ativo = TRUE
              AND (
                  @especialidade IS NULL
                  OR m.especialidade = @especialidade
              )
            ORDER BY u.nome
            """;

        using var con = new MySqlConnection(_conexao);
        con.Open();

        using var cmd = new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@especialidade",
            string.IsNullOrWhiteSpace(especialidade)
                ? DBNull.Value
                : especialidade.Trim());

        var medicos = new List<MedicoListaDto>();

        using var leitor = cmd.ExecuteReader();

        while (leitor.Read())
        {
            medicos.Add(new MedicoListaDto(
                leitor.GetInt32("id_medico"),
                leitor.GetString("nome"),
                leitor.GetString("especialidade")
            ));
        }

        return medicos;
    }
    // CONSULTA AS ESPECIALIDADES DOS MÉDICOS ATIVOS
    public List<string> ListarEspecialidades()
    {
        const string sql = """
        SELECT DISTINCT TRIM(m.especialidade) AS especialidade
        FROM medico m
        INNER JOIN usuario u
            ON m.id_usuario = u.id_usuario
        WHERE u.ativo = TRUE
          AND m.especialidade IS NOT NULL
          AND TRIM(m.especialidade) <> ''
        ORDER BY especialidade
        """;

        using var con = new MySqlConnection(_conexao);
        con.Open();

        using var cmd = new MySqlCommand(sql, con);

        var especialidades = new List<string>();

        using var leitor = cmd.ExecuteReader();

        while (leitor.Read())
        {
            especialidades.Add(
                leitor.GetString("especialidade"));
        }

        return especialidades;
    }

}

public record MedicoListaDto(
    int IdMedico,
    string Nome,
    string Especialidade
);