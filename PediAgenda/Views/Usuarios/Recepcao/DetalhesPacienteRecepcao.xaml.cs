
namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(PacienteDaApi), "PacienteDaApi")]
public partial class DetalhesPacienteRecepcao : ContentPage
{
    private int _pacienteId;

    private PacienteRecepcaoItem? _paciente;

    // ============================================
    // IDENTIFICAÇÃO DO PACIENTE
    // ============================================

    public string PacienteId
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
            {
                _pacienteId = id;

                if (NomePacienteLabel != null)
                {
                    CarregarPaciente();
                }
            }
        }
    }

    private PacienteRecepcaoItem? _pacienteDaApi;

    public PacienteRecepcaoItem? PacienteDaApi
    {
        get => _pacienteDaApi;

        set
        {
            _pacienteDaApi = value;

            if (value is not null)
            {
                _pacienteId = value.Id;
            }
        }
    }

    public DetalhesPacienteRecepcao()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_pacienteId != 0)
        {
            CarregarPaciente();
        }
    }

    // ============================================
    // CARREGAR DADOS DO PACIENTE
    // ============================================

    private void CarregarPaciente()
    {
        _paciente = PacienteDaApi
    ?? PacientesRecepcaoDados.Pacientes
        .FirstOrDefault(p => p.Id == _pacienteId);

        if (_paciente == null)
        {
            NomePacienteLabel.Text = "Paciente não encontrado";
            NascimentoLabel.Text = string.Empty;
            StatusPacienteLabel.Text = string.Empty;

            ResponsavelLabel.Text = string.Empty;
            TelefoneLabel.Text = string.Empty;
            EmailLabel.Text = string.Empty;

            return;
        }

        // DADOS DO PACIENTE

        NomePacienteLabel.Text =
            _paciente.Nome;

        NascimentoLabel.Text =
            $"Nascimento: {_paciente.DataNascimento:dd/MM/yyyy}";

        StatusPacienteLabel.Text =
            $"Status: {_paciente.Status}";

        // DADOS DO RESPONSÁVEL

        ResponsavelLabel.Text =
            _paciente.Responsavel;

        TelefoneLabel.Text =
            $"Telefone: {_paciente.TelefoneResponsavel}";

        EmailLabel.Text =
            $"E-mail: {_paciente.EmailResponsavel}";
    }

    // ============================================
    // AGENDAR CONSULTA
    // ============================================

    private async void AgendarConsultaButton_Clicked(object sender, EventArgs e)
    {
        if (_pacienteDaApi is null)
        {
            await DisplayAlertAsync("Atenção",
                "Selecione o paciente na listagem atualizada do banco de dados.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(AgendarConsultaRecepcao),
            new Dictionary<string, object>
            {
                ["PacienteDaApi"] = _pacienteDaApi
            });
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
