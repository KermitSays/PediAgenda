using MySqlConnector;

namespace PediAgenda.Nucleo.Repositorios;

public record ResultadoPeriodoAgenda(int HorariosAlterados, int ConsultasCanceladas);

public sealed class PeriodoAgendaException : Exception
{
    public PeriodoAgendaException(string mensagem) : base(mensagem)
    {
    }
}

public record PeriodoBloqueadoAgenda(DateOnly Data, TimeOnly Inicio, TimeOnly Fim, int QuantidadeHorarios);

public sealed class BloqueioPeriodoRepositorioMySql
{
    private readonly string conexao;
    private readonly TimeProvider relogio;

    public BloqueioPeriodoRepositorioMySql(string stringDeConexao, TimeProvider? relogio = null)
    {
        conexao = stringDeConexao;
        this.relogio = relogio ?? PediAgenda.Nucleo.RelogioClinica.Instancia;
    }

    public ResultadoPeriodoAgenda Bloquear(
        int idMedico,
        DateOnly dataInicial,
        DateOnly dataFinal,
        TimeOnly horarioInicial,
        TimeOnly horarioFinal)
    {
        return AlterarPeriodo(
            idMedico, dataInicial, dataFinal,
            horarioInicial, horarioFinal, bloquear: true);
    }

    public ResultadoPeriodoAgenda Liberar(
        int idMedico,
        DateOnly dataInicial,
        DateOnly dataFinal,
        TimeOnly horarioInicial,
        TimeOnly horarioFinal)
    {
        return AlterarPeriodo(
            idMedico, dataInicial, dataFinal,
            horarioInicial, horarioFinal, bloquear: false);
    }

    // Identifica sequencias de horarios bloqueados do medico no banco.
    // Sem tabela de bloqueios, um periodo que cruza dias aparece dividido por dia.
    public IReadOnlyList<PeriodoBloqueadoAgenda> ListarBloqueados(int idMedico)
    {
        if (idMedico <= 0)
            throw new PeriodoAgendaException("Médico inválido.");

        DateTime agora = relogio.GetLocalNow().DateTime;

        using var con = new MySqlConnection(conexao);
        con.Open();

        using var cmd = new MySqlCommand("""
            SELECT h.data_horario, h.hora_inicio, h.hora_fim
            FROM horario h
            INNER JOIN agenda_medica a ON a.id_agenda = h.id_agenda
            WHERE a.id_medico = @medico
              AND h.disponivel = FALSE
              AND (h.data_horario > @hoje
                   OR (h.data_horario = @hoje AND h.hora_fim > @agora))
            ORDER BY h.data_horario, h.hora_inicio, h.hora_fim, h.id_horario
            """, con);

        cmd.Parameters.AddWithValue("@medico", idMedico);
        cmd.Parameters.AddWithValue("@hoje", DateOnly.FromDateTime(agora));
        cmd.Parameters.AddWithValue("@agora", TimeOnly.FromDateTime(agora).ToTimeSpan());

        var periodos = new List<PeriodoBloqueadoAgenda>();
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            DateOnly dia = r.GetDateOnly("data_horario");
            TimeOnly inicio = TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_inicio"));
            TimeOnly fim = TimeOnly.FromTimeSpan(r.GetTimeSpan("hora_fim"));

            if (periodos.Count > 0)
            {
                var anterior = periodos[^1];
                if (anterior.Data == dia && anterior.Fim == inicio)
                {
                    periodos[^1] = anterior with
                    {
                        Fim = fim,
                        QuantidadeHorarios = anterior.QuantidadeHorarios + 1
                    };
                    continue;
                }
            }

            periodos.Add(new PeriodoBloqueadoAgenda(dia, inicio, fim, 1));
        }

        return periodos;
    }

    private ResultadoPeriodoAgenda AlterarPeriodo(
        int idMedico,
        DateOnly dataInicial,
        DateOnly dataFinal,
        TimeOnly horarioInicial,
        TimeOnly horarioFinal,
        bool bloquear)
    {
        if (idMedico <= 0 ||
            dataInicial.ToDateTime(horarioInicial) >= dataFinal.ToDateTime(horarioFinal))
        {
            throw new PeriodoAgendaException("Selecione um médico e um período válido.");
        }

        if (bloquear)
        {
            DateTime agora = relogio.GetLocalNow().DateTime;
            DateOnly hoje = DateOnly.FromDateTime(agora);

            if (dataInicial < hoje ||
                (dataInicial == hoje && horarioInicial <= TimeOnly.FromDateTime(agora)))
            {
                throw new PeriodoAgendaException(
                    "Só é possível bloquear períodos que ainda não começaram.");
            }
        }

        using var con = new MySqlConnection(conexao);
        con.Open();
        using var tx = con.BeginTransaction();

        // Trava todos os horários do período na mesma transação.
        // Uma reserva concorrente também trava a linha de horario.
        var horarios = new List<int>();

        using (var consulta = new MySqlCommand("""
            SELECT h.id_horario, h.data_horario, h.hora_inicio
            FROM horario h
            INNER JOIN agenda_medica a ON a.id_agenda = h.id_agenda
            WHERE a.id_medico = @medico
              AND h.data_horario BETWEEN @dataInicial AND @dataFinal
              AND (h.data_horario > @dataInicial OR h.hora_fim > @horaInicial)
              AND (h.data_horario < @dataFinal OR h.hora_inicio < @horaFinal)
            ORDER BY h.data_horario, h.hora_inicio, h.id_horario
            FOR UPDATE
            """, con, tx))
        {
            AdicionarPeriodo(consulta, idMedico, dataInicial, dataFinal, horarioInicial, horarioFinal);

            using var leitor = consulta.ExecuteReader();
            while (leitor.Read())
            {
                if (bloquear && leitor.GetDateOnly("data_horario").ToDateTime(
                    TimeOnly.FromTimeSpan(leitor.GetTimeSpan("hora_inicio"))) <=
                    relogio.GetLocalNow().DateTime)
                    throw new PeriodoAgendaException("O período atinge um horário que já começou. Nenhuma alteração foi realizada.");
                horarios.Add(leitor.GetInt32("id_horario"));
            }
        }

        if (horarios.Count == 0)
        {
            throw new PeriodoAgendaException(
                "Não existem horários cadastrados para esse médico no período selecionado.");
        }

        // O bloqueio original do PediAgenda cancela consultas futuras afetadas.
        // Nunca alteramos consultas realizadas ou com situação desconhecida.
        int consultasCanceladas = 0;
        using (var consulta = new MySqlCommand("""
            SELECT c.status
            FROM consulta c
            INNER JOIN horario h ON h.id_horario = c.id_horario
            INNER JOIN agenda_medica a ON a.id_agenda = h.id_agenda
            WHERE a.id_medico = @medico
              AND h.data_horario BETWEEN @dataInicial AND @dataFinal
              AND (h.data_horario > @dataInicial OR h.hora_fim > @horaInicial)
              AND (h.data_horario < @dataFinal OR h.hora_inicio < @horaFinal)
              AND c.status <> 'CANCELADA'
            FOR UPDATE
            """, con, tx))
        {
            AdicionarPeriodo(consulta, idMedico, dataInicial, dataFinal, horarioInicial, horarioFinal);

            using var leitor = consulta.ExecuteReader();
            while (leitor.Read())
            {
                string status = leitor.GetString("status");
                if (!bloquear ||
                    (status != "AGENDADA" && status != "CONFIRMADA"))
                {
                    throw new PeriodoAgendaException(
                        bloquear
                            ? "O período contém uma consulta que não pode ser cancelada automaticamente. Nenhuma alteração foi realizada."
                            : "O período contém uma consulta ativa. Nenhuma alteração foi realizada.");
                }
            }
        }

        if (bloquear)
        {
            using var cancelar = new MySqlCommand("""
                UPDATE consulta c
                INNER JOIN horario h ON h.id_horario = c.id_horario
                INNER JOIN agenda_medica a ON a.id_agenda = h.id_agenda
                SET c.status = 'CANCELADA'
                WHERE a.id_medico = @medico
                  AND h.data_horario BETWEEN @dataInicial AND @dataFinal
                  AND (h.data_horario > @dataInicial OR h.hora_fim > @horaInicial)
                  AND (h.data_horario < @dataFinal OR h.hora_inicio < @horaFinal)
                  AND c.status IN ('AGENDADA', 'CONFIRMADA')
                """, con, tx);

            AdicionarPeriodo(cancelar, idMedico, dataInicial, dataFinal, horarioInicial, horarioFinal);
            consultasCanceladas = cancelar.ExecuteNonQuery();
        }

        // Bloquear ou liberar TODOS os horários na mesma transação.
        // Se alguma operação falhar, nem cancelamentos nem bloqueios são gravados.
        using var atualizar = new MySqlCommand("""
            UPDATE horario h
            INNER JOIN agenda_medica a ON a.id_agenda = h.id_agenda
            SET h.disponivel = @disponivel
            WHERE a.id_medico = @medico
              AND h.data_horario BETWEEN @dataInicial AND @dataFinal
              AND (h.data_horario > @dataInicial OR h.hora_fim > @horaInicial)
              AND (h.data_horario < @dataFinal OR h.hora_inicio < @horaFinal)
              AND h.disponivel <> @disponivel
            """, con, tx);

        AdicionarPeriodo(atualizar, idMedico, dataInicial, dataFinal, horarioInicial, horarioFinal);
        atualizar.Parameters.AddWithValue("@disponivel", !bloquear);
        int alterados = atualizar.ExecuteNonQuery();

        tx.Commit();
        return new ResultadoPeriodoAgenda(alterados, consultasCanceladas);
    }

    private static void AdicionarPeriodo(
        MySqlCommand cmd,
        int idMedico,
        DateOnly dataInicial,
        DateOnly dataFinal,
        TimeOnly horarioInicial,
        TimeOnly horarioFinal)
    {
        cmd.Parameters.AddWithValue("@medico", idMedico);
        cmd.Parameters.AddWithValue("@dataInicial", dataInicial);
        cmd.Parameters.AddWithValue("@dataFinal", dataFinal);
        cmd.Parameters.AddWithValue("@horaInicial", horarioInicial.ToTimeSpan());
        cmd.Parameters.AddWithValue("@horaFinal", horarioFinal.ToTimeSpan());
    }
}

