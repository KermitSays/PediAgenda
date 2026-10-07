using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class BloquearHorario : ContentPage
{
    // Temporário enquanto a identidade do médico
    // ainda não vem da integração da API.
    private const int IdMedicoAgendaLocal =
        1;


    public BloquearHorario()
    {
        InitializeComponent();


        DataInicialPicker.Date =
            DateTime.Today;


        DataFinalPicker.Date =
            DateTime.Today;


        HorarioInicialPicker.Time =
            new TimeSpan(
                12,
                0,
                0);


        HorarioFinalPicker.Time =
            new TimeSpan(
                12,
                0,
                0);
    }


    // =============================================
    // CONSULTAS AFETADAS
    // =============================================

    private List<ConsultaMedico>
        EncontrarConsultasAfetadas(
            DateTime dataInicial,
            DateTime dataFinal,
            TimeSpan horarioInicial,
            TimeSpan horarioFinal)
    {
        List<ConsultaMedico>
            consultasAfetadas =
                new();


        foreach (
            ConsultaMedico consulta
            in ConsultasMedicoDados.Consultas)
        {
            bool dataDentroDoBloqueio =
                consulta.Data.Date >=
                    dataInicial.Date

                &&

                consulta.Data.Date <=
                    dataFinal.Date;


            if (!dataDentroDoBloqueio)
                continue;


            bool horarioDentroDoBloqueio =
                consulta.Horario >=
                    horarioInicial

                &&

                consulta.Horario <
                    horarioFinal;


            if (!horarioDentroDoBloqueio)
                continue;


            bool consultaEncerrada =
                consulta.Status.Equals(
                    "Cancelado",
                    StringComparison.OrdinalIgnoreCase)

                ||

                consulta.Status.Equals(
                    "Cancelada",
                    StringComparison.OrdinalIgnoreCase)

                ||

                consulta.Status.Equals(
                    "Realizada",
                    StringComparison.OrdinalIgnoreCase);


            if (consultaEncerrada)
                continue;


            consultasAfetadas.Add(
                consulta);
        }


        return consultasAfetadas;
    }


    // =============================================
    // BLOQUEAR HORÁRIO
    // =============================================

    private async void BloquearHorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        DateTime dataInicial =
            DataInicialPicker.Date
            ?? DateTime.Today;


        DateTime dataFinal =
            DataFinalPicker.Date
            ?? DateTime.Today;


        TimeSpan horarioInicial =
            HorarioInicialPicker.Time
            ?? new TimeSpan(
                12,
                0,
                0);


        TimeSpan horarioFinal =
            HorarioFinalPicker.Time
            ?? new TimeSpan(
                12,
                0,
                0);


        string motivo =
            MotivoEditor.Text?
                .Trim()
            ?? string.Empty;


        // DATA FINAL

        if (
            dataFinal <
            dataInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "A Data Final não pode ser anterior à Data Inicial.",
                "OK");


            return;
        }


        // HORÁRIO FINAL

        if (
            horarioFinal <=
            horarioInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "O Horário Final deve ser posterior ao Horário Inicial.",
                "OK");


            return;
        }


        // MOTIVO

        if (
            string.IsNullOrWhiteSpace(
                motivo))
        {
            await DisplayAlertAsync(
                "Atenção",
                "Informe o motivo do bloqueio.",
                "OK");


            return;
        }


        string periodo =
            $"{dataInicial:dd/MM/yyyy} às " +
            $"{horarioInicial:hh\\:mm} até " +
            $"{dataFinal:dd/MM/yyyy} às " +
            $"{horarioFinal:hh\\:mm}";


        List<ConsultaMedico>
            consultasAfetadas =
                EncontrarConsultasAfetadas(
                    dataInicial,
                    dataFinal,
                    horarioInicial,
                    horarioFinal);


        string mensagemConfirmacao =
            $"Deseja bloquear o horário?\n\n" +
            $"Período: {periodo}\n\n" +
            $"Motivo: {motivo}";


        if (
            consultasAfetadas.Count >
            0)
        {
            mensagemConfirmacao +=
                $"\n\nAtenção: " +
                $"{consultasAfetadas.Count} " +
                $"{(
                    consultasAfetadas.Count == 1
                        ? "consulta será afetada"
                        : "consultas serão afetadas"
                )}.";


            mensagemConfirmacao +=
                "\n\nConsultas:";


            foreach (
                ConsultaMedico consulta
                in consultasAfetadas)
            {
                mensagemConfirmacao +=
                    $"\n• " +
                    $"{consulta.Horario:hh\\:mm} - " +
                    $"{consulta.Paciente}";
            }


            mensagemConfirmacao +=
                "\n\nEssas consultas serão canceladas " +
                "e os responsáveis serão notificados.";
        }


        bool confirmar =
            await DisplayAlertAsync(
                "Confirmar bloqueio",
                mensagemConfirmacao,
                "Bloquear",
                "Cancelar");


        if (!confirmar)
            return;


        // =========================================
        // CRIA O BLOQUEIO
        // =========================================

        BloqueioHorario novoBloqueio =
            new()
            {
                DataInicial =
                    dataInicial,

                DataFinal =
                    dataFinal,

                HorarioInicial =
                    horarioInicial,

                HorarioFinal =
                    horarioFinal,

                Motivo =
                    motivo
            };


        BloqueiosMedico.Bloqueios.Add(
            novoBloqueio);


        // =========================================
        // REFLETE NA FONTE COMPARTILHADA
        // =========================================

        HorarioAgendamentoDados
            .BloquearPeriodo(
                novoBloqueio.Id,
                IdMedicoAgendaLocal,
                dataInicial,
                dataFinal,
                horarioInicial,
                horarioFinal);


        // =========================================
        // CONSULTAS AFETADAS
        // =========================================

        foreach (
            ConsultaMedico consulta
            in consultasAfetadas)
        {
            consulta.Status =
                "Cancelado";


            consulta.CanceladaPorBloqueio =
                true;


            consulta.MotivoCancelamento =
                motivo;


            // Se o horário estava reservado,
            // libera a reserva.
            //
            // Ele continua INDISPONÍVEL porque
            // agora está bloqueado.

            HorarioAgendamentoItem?
                horarioConsulta =
                    HorarioAgendamentoDados
                        .ObterPorDataHora(
                            IdMedicoAgendaLocal,
                            consulta.Data,
                            consulta.Horario);


            if (horarioConsulta != null)
            {
                HorarioAgendamentoDados
                    .LiberarHorario(
                        horarioConsulta.IdHorario);
            }


            NotificacoesDados.Notificacoes.Add(
                new Notificacao
                {
                    Titulo =
                        "Consulta cancelada",

                    Mensagem =
                        $"A consulta de " +
                        $"{consulta.Paciente} " +
                        $"do dia " +
                        $"{consulta.Data:dd/MM/yyyy} às " +
                        $"{consulta.Horario:hh\\:mm} " +
                        $"foi cancelada pelo médico " +
                        $"devido a um bloqueio de agenda." +
                        $"\n\nMotivo: {motivo}",

                    DataHora =
                        DateTime.Now,

                    Lida =
                        false
                });
        }


        await DisplayAlertAsync(
            "Horário bloqueado",
            "O horário foi bloqueado com sucesso.",
            "OK");


        await Shell.Current.GoToAsync(
            "..");
    }


    // =============================================
    // CANCELAR
    // =============================================

    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            "..");
    }
}