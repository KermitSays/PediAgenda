namespace PediAgenda.Views.Usuarios.Medico;

public partial class BloquearHorario : ContentPage
{
    public BloquearHorario()
    {
        InitializeComponent();

        DataInicialPicker.Date = DateTime.Today;
        DataFinalPicker.Date = DateTime.Today;

        HorarioInicialPicker.Time = new TimeSpan(12, 0, 0);
        HorarioFinalPicker.Time = new TimeSpan(12, 0, 0);
    }

    // Método para encontrar consultas afetadas pelo bloqueio
    private List<ConsultaMedico> EncontrarConsultasAfetadas(
    DateTime dataInicial,
    DateTime dataFinal,
    TimeSpan horarioInicial,
    TimeSpan horarioFinal)
    {
        List<ConsultaMedico> consultasAfetadas = new();

        foreach (ConsultaMedico consulta in ConsultasMedicoDados.Consultas)
        {
            bool dataDentroDoBloqueio =
                consulta.Data.Date >= dataInicial.Date &&
                consulta.Data.Date <= dataFinal.Date;

            if (!dataDentroDoBloqueio)
                continue;

            bool horarioDentroDoBloqueio =
                consulta.Horario >= horarioInicial &&
                consulta.Horario < horarioFinal;

            if (!horarioDentroDoBloqueio)
                continue;

            if (consulta.Status == "Cancelado")
                continue;

            consultasAfetadas.Add(consulta);
        }

        return consultasAfetadas;
    }

    // Evento do botão "Bloquear Horário"
    private async void BloquearHorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        DateTime dataInicial = DataInicialPicker.Date ?? DateTime.Today;
        DateTime dataFinal = DataFinalPicker.Date ?? DateTime.Today;

        TimeSpan horarioInicial = HorarioInicialPicker.Time ?? new TimeSpan(12, 0, 0);
        TimeSpan horarioFinal = HorarioFinalPicker.Time ?? new TimeSpan(12, 0, 0);

        string motivo = MotivoEditor.Text?.Trim() ?? string.Empty;

        // Valida as datas
        if (dataFinal < dataInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "A Data Final não pode ser anterior à Data Inicial.",
                "OK");

            return;
        }

        // Valida os horários
        if (dataInicial == dataFinal &&
            horarioFinal <= horarioInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "O Horário Final deve ser posterior ao Horário Inicial.",
                "OK");

            return;
        }

        // Valida o motivo
        if (string.IsNullOrWhiteSpace(motivo))
        {
            await DisplayAlertAsync(
                "Atenção",
                "Informe o motivo do bloqueio.",
                "OK");

            return;
        }

        // Monta a mensagem de confirmação
        string periodo =
            $"{dataInicial:dd/MM/yyyy} às {horarioInicial:hh\\:mm} " +
            $"até {dataFinal:dd/MM/yyyy} às {horarioFinal:hh\\:mm}";

        // Encontra as consultas afetadas pelo bloqueio
        List<ConsultaMedico> consultasAfetadas =
            EncontrarConsultasAfetadas(
                dataInicial,
                dataFinal,
                horarioInicial,
                horarioFinal);

        // Monta a mensagem de confirmação com as consultas afetadas
        string mensagemConfirmacao =
            $"Deseja bloquear o horário?\n\n" +
            $"Período: {periodo}\n\n" +
            $"Motivo: {motivo}";

        if (consultasAfetadas.Count > 0)
        {
            mensagemConfirmacao +=
                $"\n\nAtenção: {consultasAfetadas.Count} " +
                $"{(consultasAfetadas.Count == 1 ? "consulta será afetada" : "consultas serão afetadas")}.";

            mensagemConfirmacao += "\n\nConsultas:";

            foreach (ConsultaMedico consulta in consultasAfetadas)
            {
                mensagemConfirmacao +=
                    $"\n• {consulta.Horario:hh\\:mm} - {consulta.Paciente}";
            }

            mensagemConfirmacao +=
                "\n\nEssas consultas serão canceladas e os responsáveis serão notificados.";
        }

        // Solicita confirmação do bloqueio
        bool confirmar = await DisplayAlertAsync(
            "Confirmar bloqueio",
            mensagemConfirmacao,
            "Bloquear",
            "Cancelar");

        if (!confirmar)
            return;

        foreach (ConsultaMedico consulta in consultasAfetadas)
        {
            consulta.Status = "Cancelado";
            consulta.CanceladaPorBloqueio = true;
            consulta.MotivoCancelamento = motivo;
        }

        BloqueiosMedico.Bloqueios.Add(
            new BloqueioHorario
            {
                DataInicial = dataInicial,
                DataFinal = dataFinal,
                HorarioInicial = horarioInicial,
                HorarioFinal = horarioFinal,
                Motivo = motivo
            });

        await DisplayAlertAsync(
            "Horário bloqueado",
            "O horário foi bloqueado com sucesso.",
            "OK");

        await Shell.Current.GoToAsync("..");
    }

    // Evento do botão "Cancelar"
    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}