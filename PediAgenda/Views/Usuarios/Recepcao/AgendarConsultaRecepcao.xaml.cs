using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(HorarioId), "HorarioId")]
public partial class AgendarConsultaRecepcao : ContentPage
{
    private int _pacienteId;

    private int? _horarioOriginalId;

    private PacienteRecepcaoItem? _paciente;

    private HorarioAgendaRecepcao? _horarioSelecionado;


    public ObservableCollection<HorarioAgendaRecepcao>
        HorariosDisponiveis
    { get; set; } = new();


    public string PacienteId
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                _pacienteId = id;

                CarregarPaciente();
            }
        }
    }


    public string HorarioId
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                _horarioOriginalId = id;

                PrepararReagendamento();
            }
        }
    }


    public AgendarConsultaRecepcao()
    {
        InitializeComponent();

        BindingContext = this;

        CarregarMedicos();

        // Data usada atualmente nos mocks
        DataPicker.Date = new DateTime(2026, 10, 5);
    }


    private void CarregarPaciente()
    {
        _paciente =
            PacientesRecepcaoDados.Pacientes
                .FirstOrDefault(p => p.Id == _pacienteId);


        if (_paciente == null)
            return;


        PacienteLabel.Text =
            _paciente.Nome;


        ResponsavelLabel.Text =
            $"Responsável: {_paciente.Responsavel}";
    }


    private void CarregarMedicos()
    {
        MedicoPicker.Items.Clear();


        foreach (var medico in AgendaMedicaRecepcaoDados.Medicos)
        {
            MedicoPicker.Items.Add(medico);
        }


        if (MedicoPicker.Items.Count > 0)
        {
            MedicoPicker.SelectedIndex = 0;
        }
    }


    private void PrepararReagendamento()
    {
        if (!_horarioOriginalId.HasValue)
            return;


        var horarioOriginal =
            AgendaMedicaRecepcaoDados.Horarios
                .FirstOrDefault(h =>
                    h.Id == _horarioOriginalId.Value);


        if (horarioOriginal == null)
            return;


        TituloLabel.Text =
            "Reagendar consulta";


        ConfirmarButton.Text =
            "CONFIRMAR REAGENDAMENTO";


        int indiceMedico =
            AgendaMedicaRecepcaoDados.Medicos
                .IndexOf(horarioOriginal.Medico);


        if (indiceMedico >= 0)
        {
            MedicoPicker.SelectedIndex =
                indiceMedico;
        }


        DataPicker.Date =
            horarioOriginal.Data;


        CarregarHorariosDisponiveis();
    }


    private void MedicoPicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        CarregarHorariosDisponiveis();
    }


    private void DataPicker_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        CarregarHorariosDisponiveis();
    }


    private void CarregarHorariosDisponiveis()
    {
        string medico =
            MedicoPicker.SelectedItem?.ToString()
            ?? string.Empty;


        DateTime data =
            DataPicker.Date ?? DateTime.Today;


        var horarios =
            AgendaMedicaRecepcaoDados.Horarios
                .Where(h =>
                    h.Medico == medico &&
                    h.Data.Date == data.Date &&
                    h.Status == "Disponível")
                .OrderBy(h => h.Horario)
                .ToList();


        HorariosDisponiveis.Clear();


        foreach (var horario in horarios)
        {
            HorariosDisponiveis.Add(horario);
        }


        _horarioSelecionado = null;

        HorarioSelecionadoLabel.Text =
            "Nenhum";
    }


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
            _horarioSelecionado.HorarioFormatado;
    }


    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_paciente == null)
            return;


        if (_horarioSelecionado == null)
        {
            await DisplayAlertAsync(
                "Selecione um horário",
                "Escolha um horário disponível para continuar.",
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


        await Shell.Current.GoToAsync("..");
    }


    private void AgendarConsulta()
    {
        if (_paciente == null ||
            _horarioSelecionado == null)
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


    private void ReagendarConsulta()
    {
        if (_paciente == null ||
            _horarioSelecionado == null ||
            !_horarioOriginalId.HasValue)
        {
            return;
        }


        var horarioOriginal =
            AgendaMedicaRecepcaoDados.Horarios
                .FirstOrDefault(h =>
                    h.Id == _horarioOriginalId.Value);


        if (horarioOriginal == null)
            return;


        // Libera o horário anterior
        horarioOriginal.Status =
            "Disponível";

        horarioOriginal.Paciente =
            string.Empty;


        // Ocupa o novo horário
        _horarioSelecionado.Status =
            "Agendado";

        _horarioSelecionado.Paciente =
            _paciente.Nome;


        // Atualiza a consulta no histórico do paciente
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


    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}