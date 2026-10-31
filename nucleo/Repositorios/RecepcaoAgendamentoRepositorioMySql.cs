using MySqlConnector;

namespace PediAgenda.Nucleo.Repositorios;

// Retorna o responsável ao qual o paciente já pertence no banco.
// A Recepção pode agendar para esses pacientes, sem cadastrá-los pelo aplicativo.
public sealed class RecepcaoAgendamentoRepositorioMySql(string stringDeConexao)
{
    private readonly string conexao = stringDeConexao;

    public int? IdResponsavelDoPaciente(int idPaciente)
    {
        using var con = new MySqlConnection(conexao);
        con.Open();
        using var cmd = new MySqlCommand(
            "SELECT id_responsavel FROM paciente WHERE id_paciente = @paciente", con);
        cmd.Parameters.AddWithValue("@paciente", idPaciente);
        return cmd.ExecuteScalar() is { } valor and not DBNull
            ? Convert.ToInt32(valor)
            : null;
    }
}
