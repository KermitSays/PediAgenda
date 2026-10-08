using Microsoft.Maui.Graphics;

// Evita conflito entre:
// - a propriedade "StatusConsulta"
// - a classe Dados.StatusConsulta
using StatusConsultaHelper =
    PediAgenda.Dados.StatusConsulta;

namespace PediAgenda.Views.Usuarios.Recepcao;

public class HorarioAgendaRecepcao
{
    public int Id
    {
        get;
        set;
    }


    public string Medico
    {
        get;
        set;
    } = string.Empty;


    public DateTime Data
    {
        get;
        set;
    }


    public TimeSpan Horario
    {
        get;
        set;
    }


    // =============================================
    // STATUS DO HORÁRIO
    // =============================================
    //
    // Exemplos:
    // Disponível
    // Agendado
    // Bloqueado

    public string Status
    {
        get;
        set;
    } = string.Empty;


    // =============================================
    // STATUS DA CONSULTA
    // =============================================
    //
    // Exemplos:
    // Por confirmar
    // Confirmada
    // Cancelada
    // Realizada

    public string StatusConsulta
    {
        get;
        set;
    } = string.Empty;


    public string Paciente
    {
        get;
        set;
    } = string.Empty;


    public string MotivoBloqueio
    {
        get;
        set;
    } = string.Empty;


    // =============================================
    // HORÁRIO FORMATADO
    // =============================================

    public string HorarioFormatado =>
        Horario.ToString(
            @"hh\:mm");


    // =============================================
    // TEXTO PRINCIPAL DO CARD
    // =============================================

    public string PacienteExibicao
    {
        get
        {
            if (
                Status ==
                "Agendado")
            {
                return
                    string.IsNullOrWhiteSpace(
                        Paciente)

                        ? "Horário ocupado"
                        : Paciente;
            }


            return "-----------";
        }
    }


    // =============================================
    // STATUS VISUAL
    // =============================================

    public string StatusVisual
    {
        get
        {
            if (
                Status ==
                "Disponível")
            {
                return
                    "Horário disponível";
            }


            if (
                Status ==
                "Bloqueado")
            {
                return
                    "Bloqueado";
            }


            if (
                !string.IsNullOrWhiteSpace(
                    StatusConsulta))
            {
                return
                    StatusConsultaHelper
                        .Normalizar(
                            StatusConsulta);
            }


            return "Agendado";
        }
    }


    // =============================================
    // DESCRIÇÃO
    // =============================================

    public string Descricao
    {
        get
        {
            if (
                Status ==
                "Disponível")
            {
                return
                    "Horário disponível";
            }


            if (
                Status ==
                "Bloqueado")
            {
                return
                    string.IsNullOrWhiteSpace(
                        MotivoBloqueio)

                        ? "Horário bloqueado"
                        : $"Motivo: {MotivoBloqueio}";
            }


            if (
                !string.IsNullOrWhiteSpace(
                    StatusConsulta))
            {
                return
                    StatusConsultaHelper
                        .Normalizar(
                            StatusConsulta);
            }


            return
                "Consulta agendada";
        }
    }


    // =============================================
    // COR DO STATUS
    // =============================================

    public Color StatusCor
    {
        get
        {
            if (
                Status ==
                "Disponível")
            {
                return
                    Colors.Blue;
            }


            if (
                Status ==
                "Bloqueado")
            {
                return
                    Colors.Red;
            }


            string statusNormalizado =
                StatusConsultaHelper
                    .Normalizar(
                        StatusConsulta);


            return statusNormalizado switch
            {
                StatusConsultaHelper.Confirmada =>
                    Colors.Green,

                StatusConsultaHelper.Cancelada =>
                    Colors.Red,

                StatusConsultaHelper.PorConfirmar =>
                    Colors.Orange,

                StatusConsultaHelper.Realizada =>
                    Colors.Green,

                _ =>
                    Colors.Gray
            };
        }
    }
}