namespace PediAgenda.Views.Usuarios.Responsavel;

public class HorarioAgendamentoItem
{
    public int IdHorario
    {
        get;
        set;
    }


    public int IdMedico
    {
        get;
        set;
    }


    public DateTime Data
    {
        get;
        set;
    }


    public TimeSpan HoraInicio
    {
        get;
        set;
    }


    public TimeSpan HoraFim
    {
        get;
        set;
    }


    // Indica se o horário já foi reservado.
    public bool Disponivel
    {
        get;
        set;
    }


    // Quantidade de bloqueios que atingem este horário.
    //
    // Assim, se existirem dois bloqueios sobrepostos,
    // liberar um deles não libera indevidamente o outro.
    internal int QuantidadeBloqueios
    {
        get;
        set;
    }


    public bool Bloqueado =>
        QuantidadeBloqueios > 0;


    public bool EstaDisponivel =>
        Disponivel &&
        !Bloqueado;


    public string HoraTexto =>
        HoraInicio.ToString(
            @"hh\:mm");
}


public static class HorarioAgendamentoDados
{
    private static readonly List<HorarioAgendamentoItem>
        horarios =
            new();


    private static readonly Dictionary<Guid, List<int>>
        horariosPorBloqueio =
            new();


    private static int proximoIdHorario =
        1;


    // =============================================
    // GRADE LOCAL TEMPORÁRIA
    // =============================================
    //
    // Futuramente estes registros virão da API.

    private static readonly TimeSpan[]
        gradeHorarios =
        {
            new(8, 0, 0),
            new(8, 30, 0),

            new(9, 0, 0),
            new(9, 30, 0),

            new(10, 0, 0),
            new(10, 30, 0),

            new(13, 0, 0),
            new(13, 30, 0),

            new(14, 0, 0),
            new(14, 30, 0),

            new(15, 0, 0),
            new(15, 30, 0),

            new(16, 0, 0),
            new(16, 30, 0)
        };


    // =============================================
    // TODOS OS HORÁRIOS DO DIA
    // =============================================

    public static List<HorarioAgendamentoItem>
        ObterHorariosDoDia(
            int idMedico,
            DateTime data)
    {
        data =
            data.Date;


        if (
            data.DayOfWeek ==
                DayOfWeek.Saturday

            ||

            data.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            return
                new List<HorarioAgendamentoItem>();
        }


        GarantirHorariosDoDia(
            idMedico,
            data);


        return horarios
            .Where(h =>
                h.IdMedico ==
                    idMedico

                &&

                h.Data.Date ==
                    data)
            .OrderBy(h =>
                h.HoraInicio)
            .ToList();
    }


    // =============================================
    // SOMENTE HORÁRIOS DISPONÍVEIS
    // =============================================

    public static List<HorarioAgendamentoItem>
        ObterHorariosDisponiveis(
            int idMedico,
            DateTime data)
    {
        return ObterHorariosDoDia(
                idMedico,
                data)
            .Where(h =>
                h.EstaDisponivel)
            .ToList();
    }


    // =============================================
    // HORÁRIO PELO ID
    // =============================================

    public static HorarioAgendamentoItem?
        ObterPorId(
            int idHorario)
    {
        return horarios
            .FirstOrDefault(h =>
                h.IdHorario ==
                    idHorario);
    }


    // =============================================
    // HORÁRIO PELA DATA/HORA
    // =============================================

    public static HorarioAgendamentoItem?
        ObterPorDataHora(
            int idMedico,
            DateTime data,
            TimeSpan horaInicio)
    {
        GarantirHorariosDoDia(
            idMedico,
            data.Date);


        return horarios
            .FirstOrDefault(h =>
                h.IdMedico ==
                    idMedico

                &&

                h.Data.Date ==
                    data.Date

                &&

                h.HoraInicio ==
                    horaInicio);
    }


    // =============================================
    // RESERVAR HORÁRIO
    // =============================================

    public static bool ReservarHorario(
        int idHorario)
    {
        HorarioAgendamentoItem? horario =
            ObterPorId(
                idHorario);


        if (
            horario == null

            ||

            !horario.EstaDisponivel)
        {
            return false;
        }


        horario.Disponivel =
            false;


        return true;
    }


    // =============================================
    // LIBERAR HORÁRIO RESERVADO
    // =============================================

    public static void LiberarHorario(
        int idHorario)
    {
        HorarioAgendamentoItem? horario =
            ObterPorId(
                idHorario);


        if (horario != null)
        {
            horario.Disponivel =
                true;
        }
    }


    // =============================================
    // BLOQUEAR PERÍODO
    // =============================================

    public static void BloquearPeriodo(
        Guid idBloqueio,
        int idMedico,
        DateTime dataInicial,
        DateTime dataFinal,
        TimeSpan horarioInicial,
        TimeSpan horarioFinal)
    {
        // Evita registrar duas vezes o mesmo bloqueio.

        if (horariosPorBloqueio.ContainsKey(
            idBloqueio))
        {
            LiberarBloqueio(
                idBloqueio);
        }


        List<int> horariosAfetados =
            new();


        DateTime dataAtual =
            dataInicial.Date;


        while (
            dataAtual <=
            dataFinal.Date)
        {
            // Finais de semana não possuem agenda.

            if (
                dataAtual.DayOfWeek !=
                    DayOfWeek.Saturday

                &&

                dataAtual.DayOfWeek !=
                    DayOfWeek.Sunday)
            {
                GarantirHorariosDoDia(
                    idMedico,
                    dataAtual);


                List<HorarioAgendamentoItem>
                    encontrados =
                        horarios
                            .Where(h =>
                                h.IdMedico ==
                                    idMedico

                                &&

                                h.Data.Date ==
                                    dataAtual

                                &&

                                h.HoraInicio >=
                                    horarioInicial

                                &&

                                h.HoraInicio <
                                    horarioFinal)
                            .ToList();


                foreach (
                    HorarioAgendamentoItem horario
                    in encontrados)
                {
                    horario.QuantidadeBloqueios++;


                    horariosAfetados.Add(
                        horario.IdHorario);
                }
            }


            dataAtual =
                dataAtual.AddDays(1);
        }


        horariosPorBloqueio[idBloqueio] =
            horariosAfetados;
    }


    // =============================================
    // LIBERAR BLOQUEIO
    // =============================================

    public static void LiberarBloqueio(
        Guid idBloqueio)
    {
        if (
            !horariosPorBloqueio.TryGetValue(
                idBloqueio,
                out List<int>? idsHorarios))
        {
            return;
        }


        foreach (
            int idHorario
            in idsHorarios)
        {
            HorarioAgendamentoItem? horario =
                ObterPorId(
                    idHorario);


            if (
                horario != null

                &&

                horario.QuantidadeBloqueios >
                    0)
            {
                horario.QuantidadeBloqueios--;
            }
        }


        horariosPorBloqueio.Remove(
            idBloqueio);
    }


    // =============================================
    // CRIA HORÁRIOS LOCALMENTE
    // =============================================

    private static void GarantirHorariosDoDia(
        int idMedico,
        DateTime data)
    {
        data =
            data.Date;


        if (
            data.DayOfWeek ==
                DayOfWeek.Saturday

            ||

            data.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            return;
        }


        bool horariosJaCriados =
            horarios.Any(h =>
                h.IdMedico ==
                    idMedico

                &&

                h.Data.Date ==
                    data);


        if (horariosJaCriados)
            return;


        foreach (
            TimeSpan horaInicio
            in gradeHorarios)
        {
            horarios.Add(
                new HorarioAgendamentoItem
                {
                    IdHorario =
                        proximoIdHorario++,

                    IdMedico =
                        idMedico,

                    Data =
                        data,

                    HoraInicio =
                        horaInicio,

                    HoraFim =
                        horaInicio.Add(
                            TimeSpan.FromMinutes(
                                30)),

                    Disponivel =
                        true
                });
        }
    }
}