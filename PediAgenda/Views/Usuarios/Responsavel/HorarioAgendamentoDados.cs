namespace PediAgenda.Views.Usuarios.Responsavel;

public class HorarioAgendamentoItem
{
    public int IdHorario { get; set; }

    public int IdMedico { get; set; }

    public DateTime Data { get; set; }

    public TimeSpan HoraInicio { get; set; }

    public TimeSpan HoraFim { get; set; }

    public bool Disponivel { get; set; }


    public string HoraTexto =>
        HoraInicio.ToString(@"hh\:mm");
}


public static class HorarioAgendamentoDados
{
    private static readonly List<HorarioAgendamentoItem>
        horarios = new();


    private static int proximoIdHorario = 1;


    // Esta grade é usada SOMENTE enquanto a API
    // ainda não fornece os horários do banco.
    //
    // Depois, estes dados serão substituídos
    // pelo retorno da API.
    private static readonly TimeSpan[] gradeHorarios =
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


    // RETORNA SOMENTE HORÁRIOS DISPONÍVEIS

    public static List<HorarioAgendamentoItem>
        ObterHorariosDisponiveis(
            int idMedico,
            DateTime data)
    {
        data = data.Date;


        if (data.DayOfWeek ==
                DayOfWeek.Saturday ||

            data.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            return new List<HorarioAgendamentoItem>();
        }


        GarantirHorariosDoDia(
            idMedico,
            data);


        return horarios
            .Where(h =>
                h.IdMedico == idMedico

                &&

                h.Data.Date == data

                &&

                h.Disponivel)
            .OrderBy(h =>
                h.HoraInicio)
            .ToList();
    }


    // LOCALIZA UM HORÁRIO PELO ID

    public static HorarioAgendamentoItem?
        ObterPorId(
            int idHorario)
    {
        return horarios
            .FirstOrDefault(h =>
                h.IdHorario == idHorario);
    }


    // RESERVA O HORÁRIO

    public static bool ReservarHorario(
        int idHorario)
    {
        HorarioAgendamentoItem? horario =
            ObterPorId(
                idHorario);


        if (horario == null ||
            !horario.Disponivel)
        {
            return false;
        }


        horario.Disponivel = false;


        return true;
    }


    // LIBERA O HORÁRIO

    public static void LiberarHorario(
        int idHorario)
    {
        HorarioAgendamentoItem? horario =
            ObterPorId(
                idHorario);


        if (horario != null)
        {
            horario.Disponivel = true;
        }
    }


    // CRIA LOCALMENTE OS HORÁRIOS DO DIA
    //
    // Futuramente esta parte deixa de existir,
    // pois os horários virão do banco pela API.

    private static void GarantirHorariosDoDia(
        int idMedico,
        DateTime data)
    {
        bool horariosJaCriados =
            horarios.Any(h =>
                h.IdMedico == idMedico

                &&

                h.Data.Date == data.Date);


        if (horariosJaCriados)
            return;


        foreach (TimeSpan horaInicio
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
                        data.Date,

                    HoraInicio =
                        horaInicio,

                    HoraFim =
                        horaInicio.Add(
                            TimeSpan.FromMinutes(30)),

                    Disponivel =
                        true
                });
        }
    }
}