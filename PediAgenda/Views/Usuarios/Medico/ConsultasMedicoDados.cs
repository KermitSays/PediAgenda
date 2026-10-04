namespace PediAgenda.Views.Usuarios.Medico;

public static class ConsultasMedicoDados
{
    private static DateTime ProximoDiaUtil(
        DateTime data)
    {
        DateTime resultado =
            data.Date;


        while (
            resultado.DayOfWeek ==
                DayOfWeek.Saturday ||

            resultado.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            resultado =
                resultado.AddDays(1);
        }


        return resultado;
    }


    private static DateTime AdicionarDiasUteis(
        DateTime data,
        int quantidade)
    {
        DateTime resultado =
            ProximoDiaUtil(
                data);


        int adicionados =
            0;


        while (
            adicionados <
            quantidade)
        {
            resultado =
                resultado.AddDays(1);


            if (
                resultado.DayOfWeek !=
                    DayOfWeek.Saturday &&

                resultado.DayOfWeek !=
                    DayOfWeek.Sunday)
            {
                adicionados++;
            }
        }


        return resultado;
    }


    private static readonly DateTime
        primeiraDataUtil =
            ProximoDiaUtil(
                DateTime.Today);


    public static List<ConsultaMedico>
        Consultas
    { get; } =
        new()
        {
            // =========================================
            // PRIMEIRO DIA ÚTIL
            // =========================================

            new ConsultaMedico
            {
                IdPaciente = 1,

                Data =
                    primeiraDataUtil,

                Horario =
                    new TimeSpan(
                        8,
                        0,
                        0),

                Paciente =
                    "João da Silva",

                Status =
                    "Confirmado"
            },


            new ConsultaMedico
            {
                IdPaciente = 2,

                Data =
                    primeiraDataUtil,

                Horario =
                    new TimeSpan(
                        9,
                        0,
                        0),

                Paciente =
                    "Maria Alice",

                Status =
                    "Confirmado"
            },


            new ConsultaMedico
            {
                IdPaciente = 3,

                Data =
                    primeiraDataUtil,

                Horario =
                    new TimeSpan(
                        10,
                        0,
                        0),

                Paciente =
                    "Renata Oliveira",

                Status =
                    "Por Confirmar"
            },


            new ConsultaMedico
            {
                IdPaciente = null,

                Data =
                    primeiraDataUtil,

                Horario =
                    new TimeSpan(
                        14,
                        0,
                        0),

                Paciente =
                    "Bianca",

                Status =
                    "Por Confirmar"
            },


            // =========================================
            // PRÓXIMOS DIAS ÚTEIS
            // =========================================

            new ConsultaMedico
            {
                IdPaciente = 2,

                Data =
                    AdicionarDiasUteis(
                        primeiraDataUtil,
                        1),

                Horario =
                    new TimeSpan(
                        14,
                        0,
                        0),

                Paciente =
                    "Maria Alice",

                Status =
                    "Confirmado"
            },


            new ConsultaMedico
            {
                IdPaciente = 1,

                Data =
                    AdicionarDiasUteis(
                        primeiraDataUtil,
                        2),

                Horario =
                    new TimeSpan(
                        10,
                        0,
                        0),

                Paciente =
                    "João da Silva",

                Status =
                    "Por Confirmar"
            }
        };


    // =============================================
    // CONSULTAS DE UMA DATA
    // =============================================

    public static List<ConsultaMedico>
        ObterConsultasPorData(
            DateTime data)
    {
        // FIM DE SEMANA NÃO POSSUI AGENDA

        if (
            data.DayOfWeek ==
                DayOfWeek.Saturday ||

            data.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            return new List<ConsultaMedico>();
        }


        return Consultas
            .Where(c =>
                c.Data.Date ==
                data.Date)
            .OrderBy(c =>
                c.Horario)
            .ToList();
    }


    // =============================================
    // CONSULTAS ATIVAS DE UMA DATA
    // =============================================

    public static List<ConsultaMedico>
        ObterConsultasAtivasPorData(
            DateTime data)
    {
        // FIM DE SEMANA NÃO POSSUI AGENDA

        if (
            data.DayOfWeek ==
                DayOfWeek.Saturday ||

            data.DayOfWeek ==
                DayOfWeek.Sunday)
        {
            return new List<ConsultaMedico>();
        }


        return Consultas
            .Where(c =>
                c.Data.Date ==
                    data.Date &&

                !c.Status.Equals(
                    "Cancelado",
                    StringComparison.OrdinalIgnoreCase) &&

                !c.Status.Equals(
                    "Cancelada",
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(c =>
                c.Horario)
            .ToList();
    }


    // =============================================
    // CONSULTA POR ID
    // =============================================

    public static ConsultaMedico?
        ObterConsultaPorId(
            Guid idConsulta)
    {
        return Consultas
            .FirstOrDefault(c =>
                c.Id ==
                idConsulta);
    }
}