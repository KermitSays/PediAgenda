
using PediAgenda.Dados;
using PediAgenda.Views.Usuarios.Recepcao;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(IdMedico), "IdMedico")]
public partial class BloquearHorario : ContentPage
{
    private int idMedicoAgenda = 1;
    private string idMedico = "1";

    public string IdMedico
    {
        get => idMedico;

        set
        {
            idMedico = value;

            if (int.TryParse(value, out int id) && id > 0)
            {
                idMedicoAgenda = id;
            }
        }
    }

    public BloquearHorario()
    {
        InitializeComponent();

        DataInicialPicker.Date = DateTime.Today;
        DataFinalPicker.Date = DateTime.Today;

        HorarioInicialPicker.Time = new TimeSpan(12, 0, 0);
        HorarioFinalPicker.Time = new TimeSpan(12, 30, 0);
    }

    // ============================================
    // CONSULTAS LOCAIS DO MÉDICO
    // ============================================

    private List<ConsultaMedico> EncontrarConsultasMedicoAfetadas(
        DateTime dataInicial,
        DateTime dataFinal,
        TimeSpan horarioInicial,
        TimeSpan horarioFinal)
    {
        // Os dados locais representam o médico de ID 1.
        if (idMedicoAgenda != 1)
        {
            return new List<ConsultaMedico>();
        }

        return ConsultasMedicoDados.Consultas
            .Where(consulta =>
                consulta.Data.Date >= dataInicial.Date &&
                consulta.Data.Date <= dataFinal.Date &&
                consulta.Horario >= horarioInicial &&
                consulta.Horario < horarioFinal &&
                StatusConsulta.EhAtiva(consulta.Status))
            .ToList();
    }

    // ============================================
    // CONSULTAS LOCAIS DA RECEPÇÃO
    // ============================================

    private List<(
        PacienteRecepcaoItem Paciente,
        ConsultaPacienteRecepcaoItem Consulta)>
        EncontrarConsultasRecepcaoAfetadas(
            DateTime dataInicial,
            DateTime dataFinal,
            TimeSpan horarioInicial,
            TimeSpan horarioFinal)
    {
        var resultado = new List<(
            PacienteRecepcaoItem Paciente,
            ConsultaPacienteRecepcaoItem Consulta)>();

        foreach (PacienteRecepcaoItem paciente
                 in PacientesRecepcaoDados.Pacientes)
        {
            foreach (ConsultaPacienteRecepcaoItem consulta
                     in paciente.Consultas)
            {
                if (!consulta.HorarioId.HasValue)
                {
                    continue;
                }

                int idMedicoConsulta =
                    AgendaMedicaRecepcaoDados.ObterIdMedico(
                        consulta.Medico);

                if (idMedicoConsulta != idMedicoAgenda)
                {
                    continue;
                }

                bool dentro =
                    consulta.Data.Date >= dataInicial.Date &&
                    consulta.Data.Date <= dataFinal.Date &&
                    consulta.Horario >= horarioInicial &&
                    consulta.Horario < horarioFinal;

                if (dentro &&
                    StatusConsulta.EhAtiva(consulta.Status))
                {
                    resultado.Add((paciente, consulta));
                }
            }
        }

        return resultado;
    }

    // ============================================
    // BLOQUEAR PERÍODO
    // ============================================

    private async void BloquearHorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        DateTime dataInicial =
            DataInicialPicker.Date ?? DateTime.Today;

        DateTime dataFinal =
            DataFinalPicker.Date ?? DateTime.Today;

        TimeSpan horarioInicial =
            HorarioInicialPicker.Time ??
            new TimeSpan(12, 0, 0);

        TimeSpan horarioFinal =
            HorarioFinalPicker.Time ??
            new TimeSpan(12, 30, 0);

        // Motivo automático do bloqueio simplificado.
        string motivo = "Bloqueio de agenda médica";

        // ========================================
        // VALIDAÇÕES
        // ========================================

        if (dataFinal < dataInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "A Data Final não pode ser anterior à Data Inicial.",
                "OK");

            return;
        }

        if (horarioFinal <= horarioInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "O Horário Final deve ser posterior ao Horário Inicial.",
                "OK");

            return;
        }

        // ========================================
        // CONSULTAS AFETADAS
        // ========================================

        List<ConsultaMedico> consultasMedico =
            EncontrarConsultasMedicoAfetadas(
                dataInicial,
                dataFinal,
                horarioInicial,
                horarioFinal);

        var consultasRecepcao =
            EncontrarConsultasRecepcaoAfetadas(
                dataInicial,
                dataFinal,
                horarioInicial,
                horarioFinal);

        int total =
            consultasMedico.Count + consultasRecepcao.Count;

        // ========================================
        // CONFIRMAÇÃO DO BLOQUEIO
        // ========================================

        string mensagem =
            $"Deseja bloquear o período?\n\n" +
            $"Período: {dataInicial:dd/MM/yyyy} " +
            $"{horarioInicial:hh\\:mm} até " +
            $"{dataFinal:dd/MM/yyyy} " +
            $"{horarioFinal:hh\\:mm}";

        if (total > 0)
        {
            mensagem +=
                $"\n\nAtenção: {total} " +
                (total == 1
                    ? "consulta será afetada."
                    : "consultas serão afetadas.");

            mensagem += "\n\nConsultas:";

            foreach (ConsultaMedico consulta in consultasMedico)
            {
                mensagem +=
                    $"\n• {consulta.Horario:hh\\:mm} - " +
                    $"{consulta.Paciente}";
            }

            foreach (var item in consultasRecepcao)
            {
                mensagem +=
                    $"\n• {item.Consulta.Horario:hh\\:mm} - " +
                    $"{item.Paciente.Nome}";
            }

            mensagem +=
                "\n\nAs consultas afetadas serão canceladas.";
        }

        bool confirmar = await DisplayAlertAsync(
            "Confirmar bloqueio",
            mensagem,
            "Bloquear",
            "Cancelar");

        if (!confirmar)
        {
            return;
        }

        // ========================================
        // REGISTRAR BLOQUEIO LOCAL
        // ========================================

        BloqueioHorario novoBloqueio = new()
        {
            IdMedico = idMedicoAgenda,
            DataInicial = dataInicial,
            DataFinal = dataFinal,
            HorarioInicial = horarioInicial,
            HorarioFinal = horarioFinal,
            Motivo = motivo
        };

        BloqueiosMedico.Bloqueios.Add(novoBloqueio);

        HorarioAgendamentoDados.BloquearPeriodo(
            novoBloqueio.Id,
            idMedicoAgenda,
            dataInicial,
            dataFinal,
            horarioInicial,
            horarioFinal);

        // ========================================
        // CANCELAR CONSULTAS LOCAIS DO MÉDICO
        // ========================================

        foreach (ConsultaMedico consulta in consultasMedico)
        {
            consulta.Status = StatusConsulta.Cancelada;

            consulta.CanceladaPorBloqueio = true;

            consulta.MotivoCancelamento = motivo;

            HorarioAgendamentoItem? horario =
                HorarioAgendamentoDados.ObterPorDataHora(
                    idMedicoAgenda,
                    consulta.Data,
                    consulta.Horario);

            if (horario != null)
            {
                HorarioAgendamentoDados.LiberarHorario(
                    horario.IdHorario);
            }
        }

        // ========================================
        // CANCELAR CONSULTAS LOCAIS DA RECEPÇÃO
        // ========================================

        foreach (var item in consultasRecepcao)
        {
            ConsultaPacienteRecepcaoItem consulta =
                item.Consulta;

            if (!consulta.HorarioId.HasValue)
            {
                continue;
            }

            int idHorario = consulta.HorarioId.Value;

            consulta.Status = StatusConsulta.Cancelada;

            consulta.HorarioId = null;

            HorarioAgendamentoDados.LiberarHorario(
                idHorario);
        }

        // ========================================
        // MENSAGEM FINAL
        // ========================================

        await DisplayAlertAsync(
            "Período bloqueado",
            total > 0
                ? "O período foi bloqueado e as consultas afetadas foram canceladas."
                : "O período foi bloqueado com sucesso.",
            "OK");

        await Shell.Current.GoToAsync("..");
    }

    // ============================================
    // VOLTAR
    // ============================================

    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
