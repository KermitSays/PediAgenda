using PediAgenda.Views.Usuarios.Medico;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public static class AgendaMedicaRecepcaoDados
{
    // =============================================
    // MÉDICOS ANTIGOS DA RECEPÇÃO
    // =============================================
    //
    // Mantidos temporariamente porque o fluxo
    // AgendarConsultaRecepcao ainda utiliza esta
    // lista.
    //
    // Na Etapa 4C2 ele também será migrado para
    // a fonte compartilhada.

    public static List<string> Medicos
    {
        get;
    } =
        new()
        {
            "Dr. Carlos Mendes",
            "Dra. Fernanda Lima"
        };


    // =============================================
    // MÉDICOS DA AGENDA COMPARTILHADA
    // =============================================
    //
    // Estes IDs correspondem aos mesmos IDs
    // utilizados no fluxo do Responsável.

    public static List<string> MedicosCompartilhados
    {
        get;
    } =
        new()
        {
            "Dra. Ana Oliveira",
            "Dr. Carlos Mendes"
        };


    // =============================================
    // ID DO MÉDICO
    // =============================================

    public static int ObterIdMedico(
        string medico)
    {
        return medico switch
        {
            "Dra. Ana Oliveira" =>
                1,

            "Dr. Carlos Mendes" =>
                2,

            _ =>
                0
        };
    }


    // =============================================
    // AGENDA COMPARTILHADA
    // =============================================

    public static List<HorarioAgendaRecepcao>
        ObterHorariosCompartilhados(
            string medico,
            DateTime data)
    {
        int idMedico =
            ObterIdMedico(
                medico);


        if (idMedico <= 0)
        {
            return
                new List<HorarioAgendaRecepcao>();
        }


        List<HorarioAgendamentoItem>
            horariosCompartilhados =
                HorarioAgendamentoDados
                    .ObterHorariosDoDia(
                        idMedico,
                        data);


        List<HorarioAgendaRecepcao>
            resultado =
                new();


        foreach (
            HorarioAgendamentoItem horario
            in horariosCompartilhados)
        {
            // =====================================
            // LOCALIZA BLOQUEIO
            // =====================================
            //
            // Nesta versão local, os bloqueios
            // criados pelo perfil Médico pertencem
            // ao médico local de ID 1.

            BloqueioHorario? bloqueio =
                null;


            if (
                idMedico == 1

                &&

                horario.Bloqueado)
            {
                bloqueio =
                    BloqueiosMedico.Bloqueios
                        .FirstOrDefault(b =>
                            horario.Data.Date >=
                                b.DataInicial.Date

                            &&

                            horario.Data.Date <=
                                b.DataFinal.Date

                            &&

                            horario.HoraInicio >=
                                b.HorarioInicial

                            &&

                            horario.HoraInicio <
                                b.HorarioFinal);
            }


            // =====================================
            // PROCURA PACIENTE DA RECEPÇÃO
            // =====================================

            var consultaPaciente =
                PacientesRecepcaoDados.Pacientes
                    .SelectMany(
                        paciente =>
                            paciente.Consultas
                                .Select(
                                    consulta =>
                                        new
                                        {
                                            Paciente =
                                                paciente,

                                            Consulta =
                                                consulta
                                        }))
                    .FirstOrDefault(item =>
                        item.Consulta.HorarioId ==
                            horario.IdHorario

                        &&

                        item.Consulta.Data.Date ==
                            horario.Data.Date

                        &&

                        item.Consulta.Horario ==
                            horario.HoraInicio

                        &&

                        item.Consulta.Medico.Equals(
                            medico,
                            StringComparison.OrdinalIgnoreCase)

                        &&

                        !item.Consulta.Status.Equals(
                            "Cancelado",
                            StringComparison.OrdinalIgnoreCase)

                        &&

                        !item.Consulta.Status.Equals(
                            "Cancelada",
                            StringComparison.OrdinalIgnoreCase));


            // =====================================
            // DEFINE O STATUS VISUAL
            // =====================================

            string status;


            if (horario.Bloqueado)
            {
                status =
                    "Bloqueado";
            }
            else if (!horario.Disponivel)
            {
                status =
                    "Agendado";
            }
            else
            {
                status =
                    "Disponível";
            }


            string pacienteTexto =
                consultaPaciente?
                    .Paciente
                    .Nome
                ?? string.Empty;


            if (
                status ==
                    "Agendado"

                &&

                string.IsNullOrWhiteSpace(
                    pacienteTexto))
            {
                pacienteTexto =
                    "Horário ocupado";
            }


            resultado.Add(
                new HorarioAgendaRecepcao
                {
                    Id =
                        horario.IdHorario,

                    Medico =
                        medico,

                    Data =
                        horario.Data,

                    Horario =
                        horario.HoraInicio,

                    Status =
                        status,

                    Paciente =
                        pacienteTexto,

                    MotivoBloqueio =
                        bloqueio?.Motivo
                        ?? string.Empty
                });
        }


        return resultado
            .OrderBy(h =>
                h.Horario)
            .ToList();
    }


    // =============================================
    // LOCALIZA BLOQUEIO
    // =============================================

    public static BloqueioHorario?
        ObterBloqueioCompartilhado(
            string medico,
            DateTime data,
            TimeSpan horario)
    {
        int idMedico =
            ObterIdMedico(
                medico);


        // Nesta versão local, somente o Médico
        // de ID 1 possui bloqueios compartilhados.

        if (idMedico != 1)
            return null;


        return BloqueiosMedico.Bloqueios
            .FirstOrDefault(b =>
                data.Date >=
                    b.DataInicial.Date

                &&

                data.Date <=
                    b.DataFinal.Date

                &&

                horario >=
                    b.HorarioInicial

                &&

                horario <
                    b.HorarioFinal);
    }


    // =============================================
    // DADOS ANTIGOS DA RECEPÇÃO
    // =============================================
    //
    // Ainda ficam aqui SOMENTE para não quebrar
    // AgendarConsultaRecepcao e
    // DetalhesConsultaRecepcao antes da Etapa 4C2.

    public static List<HorarioAgendaRecepcao>
        Horarios
    {
        get;
    } =
        new()
        {
            new HorarioAgendaRecepcao
            {
                Id = 1,
                Medico = "Dr. Carlos Mendes",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(8, 0, 0),
                Status = "Disponível"
            },

            new HorarioAgendaRecepcao
            {
                Id = 2,
                Medico = "Dr. Carlos Mendes",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(9, 0, 0),
                Status = "Agendado",
                Paciente = "Maria Alice"
            },

            new HorarioAgendaRecepcao
            {
                Id = 3,
                Medico = "Dr. Carlos Mendes",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(10, 0, 0),
                Status = "Bloqueado",
                MotivoBloqueio = "Reunião da equipe"
            },

            new HorarioAgendaRecepcao
            {
                Id = 4,
                Medico = "Dr. Carlos Mendes",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(11, 0, 0),
                Status = "Disponível"
            },

            new HorarioAgendaRecepcao
            {
                Id = 5,
                Medico = "Dr. Carlos Mendes",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(14, 0, 0),
                Status = "Agendado",
                Paciente = "Bianca"
            },

            new HorarioAgendaRecepcao
            {
                Id = 6,
                Medico = "Dra. Fernanda Lima",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(8, 0, 0),
                Status = "Agendado",
                Paciente = "João Pedro"
            },

            new HorarioAgendaRecepcao
            {
                Id = 7,
                Medico = "Dra. Fernanda Lima",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(9, 0, 0),
                Status = "Disponível"
            },

            new HorarioAgendaRecepcao
            {
                Id = 8,
                Medico = "Dra. Fernanda Lima",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(10, 0, 0),
                Status = "Bloqueado",
                MotivoBloqueio = "Atendimento externo"
            },

            new HorarioAgendaRecepcao
            {
                Id = 9,
                Medico = "Dra. Fernanda Lima",
                Data = new DateTime(2026, 10, 5),
                Horario = new TimeSpan(11, 0, 0),
                Status = "Disponível"
            }
        };
}