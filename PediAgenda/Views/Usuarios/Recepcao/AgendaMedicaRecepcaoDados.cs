using PediAgenda.Dados;
using PediAgenda.Views.Usuarios.Medico;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public static class AgendaMedicaRecepcaoDados
{
    // =============================================
    // MÉDICOS LOCAIS
    // =============================================
    //
    // Temporário enquanto os médicos ainda não
    // são carregados pela API.

    public static List<string>
        MedicosCompartilhados
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
    // AGENDA
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
            horarios =
                HorarioAgendamentoDados
                    .ObterHorariosDoDia(
                        idMedico,
                        data);


        List<HorarioAgendaRecepcao>
            resultado =
                new();


        foreach (
            HorarioAgendamentoItem horario
            in horarios)
        {
            BloqueioHorario? bloqueio =
                ObterBloqueioCompartilhado(
                    medico,
                    horario.Data,
                    horario.HoraInicio);


            // =====================================
            // CONSULTA DA RECEPÇÃO
            // =====================================

            var consultaRecepcao =
                PacientesRecepcaoDados.Pacientes
                    .SelectMany(
                        paciente =>
                            paciente.Consultas.Select(
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

                        !StatusConsulta.EhCancelada(
                            item.Consulta.Status));


            string paciente =
                consultaRecepcao?
                    .Paciente
                    .Nome
                ?? string.Empty;


            string statusConsulta =
                consultaRecepcao?
                    .Consulta
                    .Status
                ?? string.Empty;


            // =====================================
            // CONSULTA LOCAL DO MÉDICO
            // =====================================
            //
            // O conjunto local ConsultasMedicoDados
            // representa atualmente o médico de ID 1.

            if (
                consultaRecepcao ==
                    null

                &&

                idMedico ==
                    1)
            {
                ConsultaMedico?
                    consultaMedico =
                        ConsultasMedicoDados.Consultas
                            .FirstOrDefault(c =>
                                c.Data.Date ==
                                    horario.Data.Date

                                &&

                                c.Horario ==
                                    horario.HoraInicio

                                &&

                                !StatusConsulta.EhCancelada(
                                    c.Status));


                if (
                    consultaMedico !=
                    null)
                {
                    paciente =
                        consultaMedico.Paciente;


                    statusConsulta =
                        consultaMedico.Status;
                }
            }


            // =====================================
            // STATUS DA AGENDA
            // =====================================

            string status;


            if (
                horario.Bloqueado)
            {
                status =
                    "Bloqueado";
            }
            else if (
                !horario.Disponivel)
            {
                status =
                    "Agendado";
            }
            else
            {
                status =
                    "Disponível";
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

                    StatusConsulta =
                        statusConsulta,

                    Paciente =
                        paciente,

                    MotivoBloqueio =
                        bloqueio?
                            .Motivo
                        ?? string.Empty
                });
        }


        return resultado
            .OrderBy(h =>
                h.Horario)
            .ToList();
    }


    // =============================================
    // BLOQUEIO
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


        if (idMedico <= 0)
            return null;


        return BloqueiosMedico.Bloqueios
            .FirstOrDefault(b =>
                b.IdMedico ==
                    idMedico

                &&

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
}