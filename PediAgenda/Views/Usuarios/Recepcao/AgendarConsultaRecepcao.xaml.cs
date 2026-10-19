
using System.Collections.ObjectModel;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(HorarioId), "HorarioId")]
public partial class AgendarConsultaRecepcao : ContentPage
{
    private int _pacienteId;
    private PacienteRecepcaoItem? _paciente;
    private HorarioAgendaRecepcao? _horarioSelecionado;

    private bool ajustandoData;
    private bool operacaoConcluida;
    private bool reagendamentoSolicitado;

    public ObservableCollection<HorarioAgendaRecepcao>
        HorariosDisponiveis
    { get; set; } = new();

    // IDENTIFICAÇÃO DO PACIENTE
    public string PacienteId
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
            {
                _pacienteId = id;

                if (PacienteLabel != null)
                    CarregarPaciente();
            }
        }
    }

    // Compatibilidade com caminhos antigos.
    // O reagendamento foi retirado do escopo.
    public string HorarioId
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
                reagendamentoSolicitado = true;
        }
    }

    public AgendarConsultaRecepcao()
    {
        InitializeComponent();

        BindingContext = this;

        DateTime primeiraData =
            ProximoDiaUtil(DateTime.Today);

        DataPicker.MinimumDate = primeiraData;
        DataPicker.Date = primeiraData;

        CarregarMedicos();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (operacaoConcluida)
            return;

        // Impede que uma navegação antiga
        // execute um novo agendamento por engano.
        if (reagendamentoSolicitado)
        {
            await DisplayAlertAsync(
                "Reagendamento indisponível",
                "Para alterar uma consulta, será necessário " +
                "cancelá-la e realizar um novo agendamento.",
                "Entendi");

            await Shell.Current.GoToAsync("..");
            return;
        }

        CarregarPaciente();
        CarregarHorariosDisponiveis();
    }

    // ============================================
    // DIAS ÚTEIS
    // ============================================

    private static bool EhDiaUtil(DateTime data)
    {
        return data.DayOfWeek != DayOfWeek.Saturday &&
               data.DayOfWeek != DayOfWeek.Sunday;
    }

    private static DateTime ProximoDiaUtil(DateTime data)
    {
        DateTime resultado = data.Date;

        while (!EhDiaUtil(resultado))
        {
            resultado = resultado.AddDays(1);
        }

        return resultado;
    }

    // ============================================
    // CARREGAR PACIENTE
    // ============================================

    private void CarregarPaciente()
    {
        if (_pacienteId <= 0)
            return;

        _paciente = PacientesRecepcaoDados.Pacientes
            .FirstOrDefault(p => p.Id == _pacienteId);

        if (_paciente == null)
        {
            PacienteLabel.Text = "Paciente não encontrado";
            ResponsavelLabel.Text = string.Empty;
            return;
        }

        PacienteLabel.Text = _paciente.Nome;

        ResponsavelLabel.Text =
            $"Responsável: {_paciente.Responsavel}";
    }

    // ============================================
    // CARREGAR MÉDICOS
    // ============================================

    private void CarregarMedicos()
    {
        MedicoPicker.Items.Clear();

        foreach (string medico in
                 AgendaMedicaRecepcaoDados.MedicosCompartilhados)
        {
            MedicoPicker.Items.Add(medico);
        }

        if (MedicoPicker.Items.Count > 0)
            MedicoPicker.SelectedIndex = 0;
    }

    // ============================================
    // ALTERAÇÃO DO MÉDICO
    // ============================================

    private void MedicoPicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        CarregarHorariosDisponiveis();
    }

    // ============================================
    // ALTERAÇÃO DA DATA
    // ============================================

    private async void DataPicker_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        if (ajustandoData)
            return;

        DateTime dataSelecionada =
            e.NewDate ?? DateTime.Today;

        if (!EhDiaUtil(dataSelecionada))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "A clínica não realiza atendimentos " +
                "aos sábados e domingos.",
                "OK");

            ajustandoData = true;

            DataPicker.Date =
                ProximoDiaUtil(dataSelecionada);

            ajustandoData = false;
        }

        CarregarHorariosDisponiveis();
    }

    // ============================================
    // HORÁRIOS DISPONÍVEIS
    // ============================================

    private void CarregarHorariosDisponiveis()
    {
        HorariosDisponiveis.Clear();
        _horarioSelecionado = null;

        HorarioSelecionadoLabel.Text = "Nenhum";

        string medico =
            MedicoPicker.SelectedItem?.ToString()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(medico))
            return;

        int idMedico =
            AgendaMedicaRecepcaoDados.ObterIdMedico(medico);

        if (idMedico <= 0)
            return;

        DateTime data =
            DataPicker.Date ??
            ProximoDiaUtil(DateTime.Today);

        if (!EhDiaUtil(data))
            return;

        List<HorarioAgendamentoItem> horarios =
            HorarioAgendamentoDados
                .ObterHorariosDisponiveis(idMedico, data);

        foreach (HorarioAgendamentoItem horario in horarios)
        {
            HorariosDisponiveis.Add(
                new HorarioAgendaRecepcao
                {
                    Id = horario.IdHorario,
                    Medico = medico,
                    Data = horario.Data,
                    Horario = horario.HoraInicio,
                    Status = "Disponível"
                });
        }
    }

    // ============================================
    // SELECIONAR HORÁRIO
    // ============================================

    private void Horario_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        _horarioSelecionado =
            e.CurrentSelection.FirstOrDefault()
            as HorarioAgendaRecepcao;

        HorarioSelecionadoLabel.Text =
            _horarioSelecionado?.HorarioFormatado
            ?? "Nenhum";
    }

    // ============================================
    // CONFIRMAR AGENDAMENTO
    // ============================================

    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (operacaoConcluida)
            return;

        if (_paciente == null)
        {
            await DisplayAlertAsync(
                "Paciente não encontrado",
                "Não foi possível localizar o paciente.",
                "OK");
            return;
        }

        DateTime dataSelecionada =
            DataPicker.Date ?? DateTime.Today;

        if (!EhDiaUtil(dataSelecionada))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "Não é possível agendar consultas " +
                "aos sábados ou domingos.",
                "OK");
            return;
        }

        if (_horarioSelecionado == null)
        {
            await DisplayAlertAsync(
                "Selecione um horário",
                "Escolha um horário disponível para continuar.",
                "OK");
            return;
        }

        if (!EhDiaUtil(_horarioSelecionado.Data))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "Este horário pertence a uma data sem atendimento.",
                "OK");
            return;
        }

        bool confirmar = await DisplayAlertAsync(
            "Confirmar agendamento",

            $"{_paciente.Nome}\n" +
            $"{_horarioSelecionado.Medico}\n" +
            $"{_horarioSelecionado.Data:dd/MM/yyyy} às " +
            $"{_horarioSelecionado.HorarioFormatado}",

            "CONFIRMAR",
            "VOLTAR");

        if (!confirmar)
            return;

        bool sucesso = AgendarConsulta();

        if (!sucesso)
        {
            await DisplayAlertAsync(
                "Horário indisponível",
                "Este horário não está mais disponível. " +
                "Escolha outro horário.",
                "OK");

            CarregarHorariosDisponiveis();
            return;
        }

        operacaoConcluida = true;

        await DisplayAlertAsync(
            "Sucesso",
            "A consulta foi agendada com sucesso.",
            "OK");

        await Shell.Current.GoToAsync("..");
    }

    // ============================================
    // AGENDAR CONSULTA LOCAL
    // ============================================

    private bool AgendarConsulta()
    {
        if (_paciente == null ||
            _horarioSelecionado == null ||
            !EhDiaUtil(_horarioSelecionado.Data))
        {
            return false;
        }

        // Reserva o horário na fonte compartilhada.
        bool reservado =
            HorarioAgendamentoDados.ReservarHorario(
                _horarioSelecionado.Id);

        if (!reservado)
            return false;

        // Mantém o modelo local existente.
        // Não determina forma de pagamento.
        _paciente.Consultas.Add(
            new ConsultaPacienteRecepcaoItem
            {
                HorarioId = _horarioSelecionado.Id,
                Data = _horarioSelecionado.Data,
                Horario = _horarioSelecionado.Horario,
                Medico = _horarioSelecionado.Medico,
                Especialidade = "Pediatria Geral",

                // Campo legado: será removido do modelo
                // quando o Núcleo e o BD forem ajustados.
                TipoAtendimento = string.Empty,

                Status = "Por Confirmar"
            });

        return true;
    }

    // ============================================
    // VOLTAR
    // ============================================

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
