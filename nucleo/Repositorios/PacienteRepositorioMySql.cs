using MySqlConnector;
using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public class PacienteRepositorioMySql(string stringDeConexao) : IPacienteRepositorio
{
    private readonly string _conexao = stringDeConexao;

    private MySqlConnection Abrir()
    {
        var con = new MySqlConnection(_conexao);
        con.Open();
        return con;
    }

    public int? IdResponsavelDoUsuario(int idUsuario)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(
            "SELECT id_responsavel FROM responsavel WHERE id_usuario = @usuario", con);
        cmd.Parameters.AddWithValue("@usuario", idUsuario);
        return cmd.ExecuteScalar() is { } id and not DBNull ? Convert.ToInt32(id) : null;
    }

    public IReadOnlyList<Paciente> ListarPorResponsavel(int idResponsavel)
    {
        const string sql = """
            SELECT   id_paciente, nome, data_nascimento, id_responsavel
            FROM     paciente
            WHERE    id_responsavel = @responsavel
            ORDER BY nome
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@responsavel", idResponsavel);

        var pacientes = new List<Paciente>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            pacientes.Add(new Paciente
            {
                Id = r.GetInt32("id_paciente"),
                Nome = r.GetString("nome"),
                DataNascimento = r.GetDateOnly("data_nascimento"),
                IdResponsavel = r.GetInt32("id_responsavel")
            });
        }
        return pacientes;
    }

    public bool Existe(int idResponsavel, string nome, DateOnly dataNascimento)
    {
        const string sql = """
            SELECT COUNT(*) FROM paciente
            WHERE  id_responsavel = @responsavel AND nome = @nome AND data_nascimento = @nascimento
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@responsavel", idResponsavel);
        cmd.Parameters.AddWithValue("@nome", nome);
        cmd.Parameters.AddWithValue("@nascimento", dataNascimento);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public int Inserir(int idResponsavel, string nome, DateOnly dataNascimento)
    {
        const string sql = """
            INSERT INTO paciente (nome, data_nascimento, id_responsavel)
            VALUES (@nome, @nascimento, @responsavel)
            """;

        using var con = Abrir();
        using var tx = con.BeginTransaction();
        using (var familia = new MySqlCommand(
            "SELECT id_responsavel FROM responsavel WHERE id_responsavel = @id FOR UPDATE", con, tx))
        {
            familia.Parameters.AddWithValue("@id", idResponsavel);
            familia.ExecuteScalar();
        }
        using (var duplicado = new MySqlCommand("""
            SELECT id_paciente FROM paciente
            WHERE id_responsavel = @id AND nome = @nome AND data_nascimento = @nascimento FOR UPDATE
            """, con, tx))
        {
            duplicado.Parameters.AddWithValue("@id", idResponsavel);
            duplicado.Parameters.AddWithValue("@nome", nome);
            duplicado.Parameters.AddWithValue("@nascimento", dataNascimento);
            if (duplicado.ExecuteScalar() is not (null or DBNull)) throw new PacienteDuplicadoException();
        }
        using var cmd = new MySqlCommand(sql, con, tx);
        cmd.Parameters.AddWithValue("@nome", nome);
        cmd.Parameters.AddWithValue("@nascimento", dataNascimento);
        cmd.Parameters.AddWithValue("@responsavel", idResponsavel);
        cmd.ExecuteNonQuery();
        var id = (int)cmd.LastInsertedId;
        tx.Commit();
        return id;
    }
}
