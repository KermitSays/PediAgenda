using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(PacienteId), "PacienteId")]
public partial class DetalhesPacienteRecepcao : ContentPage
{
    private int _pacienteId;

    private PacienteRecepcaoItem? _paciente;


    public ObservableCollection<ConsultaPacienteRecepcaoItem>
        ConsultasPaciente
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


    public DetalhesPacienteRecepcao()
    {
        InitializeComponent();

        BindingContext = this;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_pacienteId != 0)
        {
            CarregarPaciente();
        }
    }


    private void CarregarPaciente()
    {
        _paciente =
            PacientesRecepcaoDados.Pacientes
                .FirstOrDefault(p => p.Id == _pacienteId);


        if (_paciente == null)
            return;


        NomePacienteLabel.Text =
            _paciente.Nome;


        NascimentoLabel.Text =
            $"Nascimento: {_paciente.DataNascimento:dd/MM/yyyy}";


        StatusPacienteLabel.Text =
            $"Status: {_paciente.Status}";


        ResponsavelLabel.Text =
            _paciente.Responsavel;


        TelefoneLabel.Text =
            $"Telefone: {_paciente.TelefoneResponsavel}";


        EmailLabel.Text =
            $"E-mail: {_paciente.EmailResponsavel}";


        ConsultasPaciente.Clear();


        foreach (var consulta in
                 _paciente.Consultas
                     .OrderByDescending(c => c.Data)
                     .ThenByDescending(c => c.Horario))
        {
            ConsultasPaciente.Add(consulta);
        }


        QuantidadeConsultasLabel.Text =
            $"{ConsultasPaciente.Count} consulta(s)";


        CarregarProximaConsulta();
    }


    private void CarregarProximaConsulta()
    {
        if (_paciente == null)
            return;


        var proxima =
            _paciente.Consultas
                .Where(c =>
                    c.Data.Date >= DateTime.Today &&
                    c.Status != "Cancelado" &&
                    c.Status != "Realizado")
                .OrderBy(c => c.Data)
                .ThenBy(c => c.Horario)
                .FirstOrDefault();


        if (proxima == null)
        {
            ProximaConsultaCard.IsVisible = false;

            SemProximaConsultaLabel.IsVisible = true;

            return;
        }


        ProximaConsultaCard.IsVisible = true;

        SemProximaConsultaLabel.IsVisible = false;


        ProximaConsultaDataLabel.Text =
            proxima.DataHoraFormatada;


        ProximaConsultaMedicoLabel.Text =
            proxima.Medico;


        ProximaConsultaEspecialidadeLabel.Text =
            $"{proxima.Especialidade} • {proxima.TipoAtendimento}";


        ProximaConsultaStatusLabel.Text =
            $"Status: {proxima.Status}";
    }


    // ACESSAR CONSULTA PELO HISTÓRICO
    private async void ConsultaHistorico_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not TapGestureRecognizer gesto ||
            gesto.CommandParameter is not ConsultaPacienteRecepcaoItem consulta)
        {
            return;
        }


        // Consultas antigas ou canceladas não possuem mais
        // um horário ativo na agenda médica.
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


        await Shell.Current.GoToAsync(
            $"{nameof(DetalhesConsultaRecepcao)}" +
            $"?HorarioId={consulta.HorarioId.Value}");
    }


    // AGENDAR NOVA CONSULTA
    private async void AgendarConsultaButton_Clicked(
    object sender,
    EventArgs e)
    {
        if (_paciente == null)
            return;


        await Shell.Current.GoToAsync(
            $"{nameof(AgendarConsultaRecepcao)}" +
            $"?PacienteId={_paciente.Id}");
    }


    // VOLTAR
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}