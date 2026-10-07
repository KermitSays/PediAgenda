using System.Collections.ObjectModel;
using PediAgenda.Views.Usuarios.Responsavel;

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

    private bool operacaoConcluida;


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


                if (_horarioOriginalId.HasValue)
                {
                    PrepararReagendamento();
                }
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


    protected override void OnAppearing()
    {
        base.OnAppearing();


        if (operacaoConcluida)
            return;


        CarregarPaciente();


        if (_horarioOriginalId.HasValue)
        {
            PrepararReagendamento();
        }
        else
        {
            CarregarHorariosDisponiveis();
        }
    }


    // =============================================
    // DIA ÚTIL
    // =============================================

    private bool EhDiaUtil(
        DateTime data)
    {
        return
            data.DayOfWeek !=
                DayOfWeek.Saturday

            &&

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


        while (!EhDiaUtil(
            resultado))
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
        if (_pacienteId <= 0)
            return;


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
            in AgendaMedicaRecepcaoDados
                .MedicosCompartilhados)
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
    // LOCALIZA CONSULTA ATUAL
    // =============================================

    private ConsultaPacienteRecepcaoItem?
        ObterConsultaAtual()
    {
        if (!_horarioOriginalId.HasValue)
            return null;


        if (_paciente != null)
        {
            ConsultaPacienteRecepcaoItem?
                consultaPaciente =
                    _paciente.Consultas
                        .FirstOrDefault(c =>
                            c.HorarioId ==
                            _horarioOriginalId.Value);


            if (consultaPaciente != null)
            {
                return consultaPaciente;
            }
        }


        return PacientesRecepcaoDados.Pacientes
            .SelectMany(p =>
                p.Consultas)
            .FirstOrDefault(c =>
                c.HorarioId ==
                _horarioOriginalId.Value);
    }


    // =============================================
    // REAGENDAMENTO
    // =============================================

    private void PrepararReagendamento()
    {
        if (!_horarioOriginalId.HasValue)
            return;


        ConsultaPacienteRecepcaoItem?
            consultaAtual =
                ObterConsultaAtual();


        if (consultaAtual == null)
            return;


        TituloLabel.Text =
            "Reagendar consulta";


        ConfirmarButton.Text =
            "CONFIRMAR REAGENDAMENTO";


        int indiceMedico =
            AgendaMedicaRecepcaoDados
                .MedicosCompartilhados
                .IndexOf(
                    consultaAtual.Medico);


        if (
            indiceMedico >=
            0)
        {
            MedicoPicker.SelectedIndex =
                indiceMedico;
        }


        DataPicker.Date =
            EhDiaUtil(
                consultaAtual.Data)

                ? consultaAtual.Data

                : ProximoDiaUtil(
                    consultaAtual.Data);


        CarregarHorariosDisponiveis();
    }


    // =============================================
    // ALTERAÇÃO DO MÉDICO
    // =============================================

    private void MedicoPicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (MedicoPicker.SelectedItem == null)
            return;


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
        HorariosDisponiveis.Clear();


        _horarioSelecionado =
            null;


        HorarioSelecionadoLabel.Text =
            "Nenhum";


        string medico =
            MedicoPicker.SelectedItem?
                .ToString()
            ?? string.Empty;


        if (
            string.IsNullOrWhiteSpace(
                medico))
        {
            return;
        }


        int idMedico =
            AgendaMedicaRecepcaoDados
                .ObterIdMedico(
                    medico);


        if (idMedico <= 0)
            return;


        DateTime data =
            DataPicker.Date
            ?? ProximoDiaUtil(
                DateTime.Today);


        if (!EhDiaUtil(data))
            return;


        List<HorarioAgendamentoItem>
            horarios =
                HorarioAgendamentoDados
                    .ObterHorariosDisponiveis(
                        idMedico,
                        data);


        foreach (
            HorarioAgendamentoItem horario
            in horarios)
        {
            HorariosDisponiveis.Add(
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
                        "Disponível"
                });
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
            DataPicker.Date
            ?? DateTime.Today;


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


        bool sucesso;


        if (_horarioOriginalId.HasValue)
        {
            sucesso =
                ReagendarConsulta();
        }
        else
        {
            sucesso =
                AgendarConsulta();
        }


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


        operacaoConcluida =
            true;


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

    private bool AgendarConsulta()
    {
        if (
            _paciente == null

            ||

            _horarioSelecionado == null

            ||

            !EhDiaUtil(
                _horarioSelecionado.Data))
        {
            return false;
        }


        // =========================================
        // RESERVA NA FONTE COMPARTILHADA
        // =========================================

        bool reservado =
            HorarioAgendamentoDados
                .ReservarHorario(
                    _horarioSelecionado.Id);


        if (!reservado)
            return false;


        // =========================================
        // CRIA A CONSULTA LOCAL DA RECEPÇÃO
        // =========================================

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


        return true;
    }


    // =============================================
    // REAGENDAR
    // =============================================

    private bool ReagendarConsulta()
    {
        if (
            _paciente == null

            ||

            _horarioSelecionado == null

            ||

            !_horarioOriginalId.HasValue

            ||

            !EhDiaUtil(
                _horarioSelecionado.Data))
        {
            return false;
        }


        ConsultaPacienteRecepcaoItem?
            consulta =
                ObterConsultaAtual();


        if (consulta == null)
            return false;


        int idHorarioAntigo =
            _horarioOriginalId.Value;


        // PRIMEIRO RESERVA O NOVO HORÁRIO.

        bool novoHorarioReservado =
            HorarioAgendamentoDados
                .ReservarHorario(
                    _horarioSelecionado.Id);


        if (!novoHorarioReservado)
            return false;


        // DEPOIS LIBERA O ANTIGO.

        HorarioAgendamentoDados
            .LiberarHorario(
                idHorarioAntigo);


        // ATUALIZA A CONSULTA.

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


        return true;
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