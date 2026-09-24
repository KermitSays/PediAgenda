namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
[QueryProperty(nameof(Modalidade), "Modalidade")]
[QueryProperty(nameof(Valor), "Valor")]
[QueryProperty(nameof(Status), "Status")]
public partial class MinhasConsultasDetalhes : ContentPage
{
    private string? medico;
    private string? especialidade;
    private string? paciente;
    private string? data;
    private string? horario;
    private string? modalidade;
    private string? valor;
    private string? status;

    public string Medico
    {
        get => medico ?? string.Empty;
        set
        {
            medico = value;

            if (MedicoLabel != null)
                MedicoLabel.Text = value;
        }
    }

    public string Especialidade
    {
        get => especialidade ?? string.Empty;
        set
        {
            especialidade = value;

            if (EspecialidadeLabel != null)
                EspecialidadeLabel.Text = value;
        }
    }

    public string Paciente
    {
        get => paciente ?? string.Empty;
        set
        {
            paciente = value;

            if (PacienteLabel != null)
                PacienteLabel.Text = value;
        }
    }

    public string Data
    {
        get => data ?? string.Empty;
        set
        {
            data = value;

            if (DataLabel != null)
                DataLabel.Text = value;
        }
    }

    public string Horario
    {
        get => horario ?? string.Empty;
        set
        {
            horario = value;

            if (HorarioLabel != null)
                HorarioLabel.Text = value;
        }
    }

    public string Modalidade
    {
        get => modalidade ?? string.Empty;
        set
        {
            modalidade = value;

            if (ModalidadeLabel != null)
                ModalidadeLabel.Text = value;
        }
    }

    public string Valor
    {
        get => valor ?? string.Empty;
        set
        {
            valor = value;

            if (ValorLabel != null)
                ValorLabel.Text = value;
        }
    }

    public string Status
    {
        get => status ?? string.Empty;
        set
        {
            status = value;

            if (StatusLabel != null)
                StatusLabel.Text = value;
        }
    }

    public MinhasConsultasDetalhes()
    {
        InitializeComponent();
    }

    // REAGENDAR CONSULTA
    private async void ReagendarButton_Clicked(
    object sender,
    EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(ReagendarConsulta),
            new Dictionary<string, object>
            {
            { "Medico", Medico },
            { "Especialidade", Especialidade },
            { "Paciente", Paciente }
            });
    }

    // CANCELAR CONSULTA
    private async void CancelarButton_Clicked(
    object sender,
    EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(CancelarConsulta),
            new Dictionary<string, object>
            {
            { "Medico", Medico },
            { "Especialidade", Especialidade },
            { "Paciente", Paciente },
            { "Data", Data },
            { "Horario", Horario }
            });
    }

    // VOLTAR
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}