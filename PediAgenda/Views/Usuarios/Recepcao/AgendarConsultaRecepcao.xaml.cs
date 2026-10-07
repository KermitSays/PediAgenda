using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(HorarioId), "HorarioId")]
public partial class AgendarConsultaRecepcao : ContentPage
{
    private int _pacienteId;

    private int? _horarioOriginalId;

    private PacienteRecepcaoItem?
        _paciente;

    private HorarioAgendaRecepcao?
        _horarioSelecionado;

    private bool ajustandoData;


    public ObservableCollection<HorarioAgendaRecepcao>
        HorariosDisponiveis
    {
        get;
        set;
    } = new();


    public string PacienteId
    {
        set
        {
            if (
                int.TryParse(
                    value,
                    out int id))
            {
                _pacienteId =
                    id;


                CarregarPaciente();
            }
        }
    }


    public string HorarioId
    {
        set
        {
            if (
                int.TryParse(
                    value,
                    out int id))
            {
                _horarioOriginalId =
                    id;


                PrepararReagendamento();
            }
        }
    }


    public AgendarConsultaRecepcao()
    {
        InitializeComponent();


        BindingContext =
            this;


        CarregarMedicos();


        DateTime primeiraDataDisponivel =
            ProximoDiaUtil(
                DateTime.Today);


        DataPicker.MinimumDate =
            primeiraDataDisponivel;


        DataPicker.Date =
            primeiraDataDisponivel;
    }


    // =============================================
    // DIA ÚTIL
    // =============================================

    private bool EhDiaUtil(
        DateTime data)
    {
        return
            data.DayOfWeek !=
                DayOfWeek.Saturday &&

            data.DayOfWeek !=
                DayOfWeek.Sunday;
    }


    // =============================================
    // PRÓXIMO DIA ÚTIL
    // =============================================

    private DateTime ProximoDiaUtil(
        DateTime data)
    {
        DateTime resultado =
            data.Date;


        while (!EhDiaUtil(resultado))
        {
            resultado =
                resultado.AddDays(1);
        }


        return resultado;
    }


    // =============================================
    // PACIENTE
    // =============================================

    private void CarregarPaciente()
    {
        _paciente =
            PacientesRecepcaoDados.Pacientes
                .FirstOrDefault(p =>
                    p.Id ==
                    _pacienteId);


        if (_paciente == null)
            return;


        PacienteLabel.Text =
            _paciente.Nome;


        ResponsavelLabel.Text =
            $"Responsável: " +
            $"{_paciente.Responsavel}";
    }


    // =============================================
    // MÉDICOS
    // =============================================

    private void CarregarMedicos()
    {
        MedicoPicker.Items.Clear();


        foreach (
            string medico
            in AgendaMedicaRecepcaoDados.Medicos)
        {
            MedicoPicker.Items.Add(
                medico);
        }


        if (
            MedicoPicker.Items.Count >
            0)
        {
            MedicoPicker.SelectedIndex =
                0;
        }
    }


    // =============================================
    // REAGENDAMENTO
    // =============================================

    private void PrepararReagendamento()
    {
        if (!_horarioOriginalId.HasValue)
            return;


        var horarioOriginal =
            AgendaMedicaRecepcaoDados.Horarios
                .FirstOrDefault(h =>
                    h.Id ==
                    _horarioOriginalId.Value);


        if (horarioOriginal == null)
            return;


        TituloLabel.Text =
            "Reagendar consulta";


        ConfirmarButton.Text =
            "CONFIRMAR REAGENDAMENTO";


        int indiceMedico =
            AgendaMedicaRecepcaoDados.Medicos
                .IndexOf(
                    horarioOriginal.Medico);


        if (
            indiceMedico >=
            0)
        {
            MedicoPicker.SelectedIndex =
                indiceMedico;
        }


        // SE ALGUM DADO ANTIGO ESTIVER EM
        // FIM DE SEMANA, MOVE PARA O PRÓXIMO
        // DIA ÚTIL.

        DataPicker.Date =
            EhDiaUtil(
                horarioOriginal.Data)

                ? horarioOriginal.Data

                : ProximoDiaUtil(
                    horarioOriginal.Data);


        CarregarHorariosDisponiveis();
    }


    // =============================================
    // ALTERAÇÃO DO MÉDICO
    // =============================================

    private void MedicoPicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        CarregarHorariosDisponiveis();
    }


    // =============================================
    // ALTERAÇÃO DA DATA
    // =============================================

    private async void DataPicker_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        if (ajustandoData)
            return;


        DateTime dataSelecionada =
            e.NewDate
            ?? DateTime.Today;


        if (!EhDiaUtil(
            dataSelecionada))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "A clínica não realiza atendimentos aos sábados e domingos.",
                "OK");


            ajustandoData =
                true;


            DataPicker.Date =
                ProximoDiaUtil(
                    dataSelecionada);


            ajustandoData =
                false;
        }


        CarregarHorariosDisponiveis();
    }


    // =============================================
    // HORÁRIOS DISPONÍVEIS
    // =============================================

    private void CarregarHorariosDisponiveis()
    {
        string medico =
            MedicoPicker.SelectedItem?
                .ToString()
            ?? string.Empty;


        DateTime data =
            DataPicker.Date
            ?? ProximoDiaUtil(
                DateTime.Today);


        HorariosDisponiveis.Clear();


        _horarioSelecionado =
            null;


        HorarioSelecionadoLabel.Text =
            "Nenhum";


        // NÃO CARREGA HORÁRIOS EM FIM DE SEMANA

        if (!EhDiaUtil(data))
        {
            return;
        }


        var horarios =
            AgendaMedicaRecepcaoDados.Horarios
                .Where(h =>
                    h.Medico ==
                        medico &&

                    h.Data.Date ==
                        data.Date &&

                    h.Status ==
                        "Disponível")
                .OrderBy(h =>
                    h.Horario)
                .ToList();


        foreach (
            HorarioAgendaRecepcao horario
            in horarios)
        {
            HorariosDisponiveis.Add(
                horario);
        }
    }


    // =============================================
    // SELEÇÃO DO HORÁRIO
    // =============================================

    private void Horario_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        _horarioSelecionado =
            e.CurrentSelection
                .FirstOrDefault()
            as HorarioAgendaRecepcao;


        if (_horarioSelecionado == null)
        {
            HorarioSelecionadoLabel.Text =
                "Nenhum";


            return;
        }


        HorarioSelecionadoLabel.Text =
            _horarioSelecionado
                .HorarioFormatado;
    }


    // =============================================
    // CONFIRMAR
    // =============================================

    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_paciente == null)
            return;


        DateTime dataSelecionada =
            DataPicker.Date
            ?? DateTime.Today;


        // VALIDAÇÃO FINAL:
        // MESMO QUE ALGO NA INTERFACE FALHE,
        // NUNCA GRAVA CONSULTA NO FIM DE SEMANA.

        if (!EhDiaUtil(
            dataSelecionada))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "Não é possível agendar consultas aos sábados ou domingos.",
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


        if (!EhDiaUtil(
            _horarioSelecionado.Data))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "Este horário pertence a uma data sem atendimento.",
                "OK");


            return;
        }


        bool confirmar =
            await DisplayAlertAsync(

                _horarioOriginalId.HasValue
                    ? "Confirmar reagendamento"
                    : "Confirmar agendamento",

                $"{_paciente.Nome}\n" +
                $"{_horarioSelecionado.Medico}\n" +

                $"{_horarioSelecionado.Data:dd/MM/yyyy} às " +
                $"{_horarioSelecionado.HorarioFormatado}",

                "CONFIRMAR",
                "VOLTAR");


        if (!confirmar)
            return;


        if (_horarioOriginalId.HasValue)
        {
            ReagendarConsulta();
        }
        else
        {
            AgendarConsulta();
        }


        await DisplayAlertAsync(
            "Sucesso",

            _horarioOriginalId.HasValue
                ? "A consulta foi reagendada com sucesso."
                : "A consulta foi agendada com sucesso.",

            "OK");


        await Shell.Current.GoToAsync(
            "..");
    }


    // =============================================
    // AGENDAR
    // =============================================

    private void AgendarConsulta()
    {
        if (
            _paciente == null ||

            _horarioSelecionado == null ||

            !EhDiaUtil(
                _horarioSelecionado.Data))
        {
            return;
        }


        _horarioSelecionado.Status =
            "Agendado";


        _horarioSelecionado.Paciente =
            _paciente.Nome;


        _paciente.Consultas.Add(
            new ConsultaPacienteRecepcaoItem
            {
                HorarioId =
                    _horarioSelecionado.Id,

                Data =
                    _horarioSelecionado.Data,

                Horario =
                    _horarioSelecionado.Horario,

                Medico =
                    _horarioSelecionado.Medico,

                Especialidade =
                    "Pediatria Geral",

                TipoAtendimento =
                    "Particular",

                Status =
                    "Por Confirmar"
            });
    }


    // =============================================
    // REAGENDAR
    // =============================================

    private void ReagendarConsulta()
    {
        if (
            _paciente == null ||

            _horarioSelecionado == null ||

            !_horarioOriginalId.HasValue ||

            !EhDiaUtil(
                _horarioSelecionado.Data))
        {
            return;
        }


        var horarioOriginal =
            AgendaMedicaRecepcaoDados.Horarios
                .FirstOrDefault(h =>
                    h.Id ==
                    _horarioOriginalId.Value);


        if (horarioOriginal == null)
            return;


        // LIBERA O HORÁRIO ANTERIOR

        horarioOriginal.Status =
            "Disponível";


        horarioOriginal.Paciente =
            string.Empty;


        // OCUPA O NOVO HORÁRIO

        _horarioSelecionado.Status =
            "Agendado";


        _horarioSelecionado.Paciente =
            _paciente.Nome;


        // ATUALIZA A CONSULTA DO PACIENTE

        var consulta =
            _paciente.Consultas
                .FirstOrDefault(c =>
                    c.HorarioId ==
                    _horarioOriginalId.Value);


        if (consulta != null)
        {
            consulta.HorarioId =
                _horarioSelecionado.Id;


            consulta.Data =
                _horarioSelecionado.Data;


            consulta.Horario =
                _horarioSelecionado.Horario;


            consulta.Medico =
                _horarioSelecionado.Medico;


            consulta.Status =
                "Por Confirmar";
        }
    }


    // =============================================
    // VOLTAR
    // =============================================

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            "..");
    }
}