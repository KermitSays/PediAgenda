using MySqlConnector;
using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public class ConsultaRepositorioMySql(string stringDeConexao) : IConsultaRepositorio
{
    private readonly string _conexao = stringDeConexao;

    private MySqlConnection Abrir()
    {
        var con = new MySqlConnection(_conexao);
        con.Open();
        return con;
    }

    public (ResultadoReserva Resultado, int IdConsulta) Reservar(
        int idResponsavel, int idPaciente, int idHorario, string tipoAtendimento, DateTime agora)
    {
        using var con = Abrir();
        // Sair do método sem Commit desfaz tudo: o using da transação faz o rollback.
        using var tx = con.BeginTransaction();

        // 1. O paciente é desta família? Se não for, a resposta é a mesma de
        //    "não existe" — não se confirma a existência de filho de outra família.
        using (var cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM paciente WHERE id_paciente = @paciente AND id_responsavel = @responsavel",
            con, tx))
        {
            cmd.Parameters.AddWithValue("@paciente", idPaciente);
            cmd.Parameters.AddWithValue("@responsavel", idResponsavel);
            if (Convert.ToInt32(cmd.ExecuteScalar()) == 0)
                return (ResultadoReserva.PacienteNaoEncontrado, 0);
        }

        // 2. Trava o horário até o fim da transação (FOR UPDATE). Um segundo
        //    pedido para o mesmo horário fica esperando aqui, e quando passa
        //    já encontra a consulta que o primeiro gravou.
        DateOnly data;
        TimeOnly inicio, fim;
        using (var cmd = new MySqlCommand(
            "SELECT data_horario, hora_inicio, hora_fim, disponivel FROM horario WHERE id_horario = @horario FOR UPDATE",
            con, tx))
        {
            cmd.Parameters.AddWithValue("@horario", idHorario);
            using var r = cmd.ExecuteReader();
            if (!r.Read())
                return (ResultadoReserva.HorarioNaoEncontrado, 0);

            data = r.GetDateOnly("data_horario");
            inicio = TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio"));
            fim = TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_fim"));

            if (data.ToDateTime(inicio) <= agora)
                return (ResultadoReserva.HorarioPassado, 0);
            if (!r.GetBoolean("disponivel"))
                return (ResultadoReserva.HorarioBloqueado, 0);
        }

        // 3. Já tem consulta ativa nele? Leitura com trava, para enxergar a
        //    versão mais recente, e não a de quando a transação começou.
        using (var cmd = new MySqlCommand("""
            SELECT COUNT(*) FROM consulta
            WHERE  id_horario = @horario AND status <> 'CANCELADA'
            FOR UPDATE
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@horario", idHorario);
            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                return (ResultadoReserva.HorarioOcupado, 0);
        }

        // 4. A criança não pode estar em dois médicos ao mesmo tempo.
        using (var cmd = new MySqlCommand("""
            SELECT COUNT(*)
            FROM   consulta c
            JOIN   horario  h ON h.id_horario = c.id_horario
            WHERE  c.id_paciente = @paciente
              AND  c.status <> 'CANCELADA'
              AND  h.data_horario = @data
              AND  h.hora_inicio < @fim
              AND  h.hora_fim    > @inicio
            FOR SHARE
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@paciente", idPaciente);
            cmd.Parameters.AddWithValue("@data", data);
            cmd.Parameters.AddWithValue("@inicio", inicio.ToTimeSpan());
            cmd.Parameters.AddWithValue("@fim", fim.ToTimeSpan());
            if (Convert.ToInt32(cmd.ExecuteScalar()) > 0)
                return (ResultadoReserva.PacienteOcupadoNoHorario, 0);
        }

        // 5. Tudo certo: grava.
        using (var cmd = new MySqlCommand("""
            INSERT INTO consulta (tipo_atendimento, id_paciente, id_horario)
            VALUES (@tipo, @paciente, @horario)
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@tipo", tipoAtendimento);
            cmd.Parameters.AddWithValue("@paciente", idPaciente);
            cmd.Parameters.AddWithValue("@horario", idHorario);
            cmd.ExecuteNonQuery();
            var id = (int)cmd.LastInsertedId;
            tx.Commit();
            return (ResultadoReserva.Reservada, id);
        }
    }

    // Consulta + horário + médico + paciente, restrito aos filhos do responsável.
    private const string SelectDetalhe = """
        SELECT   c.id_consulta, c.status, c.tipo_atendimento,
                 h.data_horario, h.hora_inicio, h.hora_fim,
                 m.id_medico, um.nome AS nome_medico, m.especialidade,
                 p.id_paciente, p.nome AS nome_paciente
        FROM     consulta      c
        JOIN     paciente      p  ON p.id_paciente  = c.id_paciente
        JOIN     horario       h  ON h.id_horario   = c.id_horario
        JOIN     agenda_medica a  ON a.id_agenda    = h.id_agenda
        JOIN     medico        m  ON m.id_medico    = a.id_medico
        JOIN     usuario       um ON um.id_usuario  = m.id_usuario
        WHERE    p.id_responsavel = @responsavel
        """;

    public IReadOnlyList<ConsultaDetalhe> ListarDoResponsavel(int idResponsavel)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(SelectDetalhe + " ORDER BY h.data_horario, h.hora_inicio", con);
        cmd.Parameters.AddWithValue("@responsavel", idResponsavel);

        var consultas = new List<ConsultaDetalhe>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            consultas.Add(Ler(r));
        return consultas;
    }

    public ConsultaDetalhe? BuscarDoResponsavel(int idResponsavel, int idConsulta)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(SelectDetalhe + " AND c.id_consulta = @consulta", con);
        cmd.Parameters.AddWithValue("@responsavel", idResponsavel);
        cmd.Parameters.AddWithValue("@consulta", idConsulta);

        using var r = cmd.ExecuteReader();
        return r.Read() ? Ler(r) : null;
    }

    public ResultadoCancelamento Cancelar(int idResponsavel, int idConsulta, DateTime agora)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();

        using (var cmd = new MySqlCommand("""
            SELECT c.status, h.data_horario, h.hora_inicio
            FROM   consulta c
            JOIN   paciente p ON p.id_paciente = c.id_paciente
            JOIN   horario  h ON h.id_horario  = c.id_horario
            WHERE  c.id_consulta = @consulta AND p.id_responsavel = @responsavel
            FOR UPDATE
            """, con, tx))
        {
            cmd.Parameters.AddWithValue("@consulta", idConsulta);
            cmd.Parameters.AddWithValue("@responsavel", idResponsavel);
            using var r = cmd.ExecuteReader();
            if (!r.Read())
                return ResultadoCancelamento.NaoEncontrada;

            var status = r.GetString("status");
            if (status is StatusConsulta.Cancelada or StatusConsulta.Realizada)
                return ResultadoCancelamento.JaEncerrada;

            var inicio = r.GetDateOnly("data_horario")
                          .ToDateTime(TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio")));
            if (inicio <= agora)
                return ResultadoCancelamento.JaPassou;
        }

        using (var cmd = new MySqlCommand(
            "UPDATE consulta SET status = 'CANCELADA' WHERE id_consulta = @consulta", con, tx))
        {
            cmd.Parameters.AddWithValue("@consulta", idConsulta);
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
        return ResultadoCancelamento.Cancelada;
    }

    private static ConsultaDetalhe Ler(MySqlDataReader r) => new(
        r.GetInt32("id_consulta"),
        r.GetString("status"),
        r.GetString("tipo_atendimento"),
        r.GetDateOnly("data_horario"),
        TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio")),
        TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_fim")),
        r.GetInt32("id_medico"),
        r.GetString("nome_medico"),
        r.GetString("especialidade"),
        r.GetInt32("id_paciente"),
        r.GetString("nome_paciente"));
}
