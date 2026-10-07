using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(PacienteId), "PacienteId")]
public partial class DetalhesPacienteRecepcao : ContentPage
{
    private int _pacienteId;


    private PacienteRecepcaoItem?
        _paciente;


    private ConsultaPacienteRecepcaoItem?
        _proximaConsulta;


    public ObservableCollection<ConsultaPacienteRecepcaoItem>
        ConsultasPaciente
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


                if (NomePacienteLabel != null)
                {
                    CarregarPaciente();
                }
            }
        }
    }


    public DetalhesPacienteRecepcao()
    {
        InitializeComponent();


        BindingContext =
            this;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        if (_pacienteId != 0)
        {
            CarregarPaciente();
        }
    }


    // =============================================
    // CARREGA PACIENTE
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


        NomePacienteLabel.Text =
            _paciente.Nome;


        NascimentoLabel.Text =
            $"Nascimento: " +
            $"{_paciente.DataNascimento:dd/MM/yyyy}";


        StatusPacienteLabel.Text =
            $"Status: {_paciente.Status}";


        ResponsavelLabel.Text =
            _paciente.Responsavel;


        TelefoneLabel.Text =
            $"Telefone: " +
            $"{_paciente.TelefoneResponsavel}";


        EmailLabel.Text =
            $"E-mail: " +
            $"{_paciente.EmailResponsavel}";


        // =========================================
        // CONSULTAS
        // =========================================

        ConsultasPaciente.Clear();


        foreach (
            ConsultaPacienteRecepcaoItem consulta
            in _paciente.Consultas
                .OrderByDescending(c =>
                    c.Data)
                .ThenByDescending(c =>
                    c.Horario))
        {
            ConsultasPaciente.Add(
                consulta);
        }


        QuantidadeConsultasLabel.Text =
            $"{ConsultasPaciente.Count} consulta(s)";


        CarregarProximaConsulta();
    }


    // =============================================
    // PRÓXIMA CONSULTA
    // =============================================

    private void CarregarProximaConsulta()
    {
        if (_paciente == null)
            return;


        _proximaConsulta =
            _paciente.Consultas
                .Where(c =>
                    c.Data.Date >=
                        DateTime.Today

                    &&

                    !c.Status.Equals(
                        "Cancelado",
                        StringComparison.OrdinalIgnoreCase)

                    &&

                    !c.Status.Equals(
                        "Cancelada",
                        StringComparison.OrdinalIgnoreCase)

                    &&

                    !c.Status.Equals(
                        "Realizado",
                        StringComparison.OrdinalIgnoreCase)

                    &&

                    !c.Status.Equals(
                        "Realizada",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(c =>
                    c.Data)
                .ThenBy(c =>
                    c.Horario)
                .FirstOrDefault();


        if (_proximaConsulta == null)
        {
            ProximaConsultaCard.IsVisible =
                false;


            SemProximaConsultaLabel.IsVisible =
                true;


            return;
        }


        ProximaConsultaCard.IsVisible =
            true;


        SemProximaConsultaLabel.IsVisible =
            false;


        ProximaConsultaDataLabel.Text =
            _proximaConsulta
                .DataHoraFormatada;


        ProximaConsultaMedicoLabel.Text =
            _proximaConsulta
                .Medico;


        ProximaConsultaEspecialidadeLabel.Text =
            $"{_proximaConsulta.Especialidade} • " +
            $"{_proximaConsulta.TipoAtendimento}";


        ProximaConsultaStatusLabel.Text =
            $"Status: " +
            $"{_proximaConsulta.Status}";
    }


    // =============================================
    // PRÓXIMA CONSULTA - CLIQUE
    // =============================================

    private async void ProximaConsultaButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_proximaConsulta == null)
            return;


        await AbrirConsultaAsync(
            _proximaConsulta);
    }


    // =============================================
    // HISTÓRICO - CLIQUE
    // =============================================

    private async void ConsultaHistoricoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            sender is not Button botao

            ||

            botao.CommandParameter
                is not ConsultaPacienteRecepcaoItem consulta)
        {
            return;
        }


        await AbrirConsultaAsync(
            consulta);
    }


    // =============================================
    // ABRIR CONSULTA
    // =============================================

    private async Task AbrirConsultaAsync(
        ConsultaPacienteRecepcaoItem consulta)
    {
        // =========================================
        // CONSULTA SEM HORÁRIO ATIVO
        // =========================================

        if (!consulta.HorarioId.HasValue)
        {
            await DisplayAlertAsync(
                "Consulta do histórico",

                $"Médico: {consulta.Medico}\n" +
                $"Data: {consulta.Data:dd/MM/yyyy}\n" +
                $"Horário: {consulta.Horario:hh\\:mm}\n" +
                $"Status: {consulta.Status}\n\n" +
                "Esta consulta não possui um horário ativo na agenda.",

                "OK");


            return;
        }


        // =========================================
        // CONSULTA ATIVA
        // =========================================

        await Shell.Current.GoToAsync(
            nameof(DetalhesConsultaRecepcao),

            new Dictionary<string, object>
            {
                {
                    "HorarioId",
                    consulta.HorarioId.Value.ToString()
                }
            });
    }


    // =============================================
    // AGENDAR NOVA CONSULTA
    // =============================================

    private async void AgendarConsultaButton_Clicked(
    object sender,
    EventArgs e)
    {
        if (_paciente == null)
            return;


        await Shell.Current.GoToAsync(
            nameof(AgendarConsultaRecepcao),

            new Dictionary<string, object>
            {
            {
                "PacienteId",
                _paciente.Id.ToString()
            }
            });
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