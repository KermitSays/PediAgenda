namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
public partial class CancelarConsulta : ContentPage
{
    private string? medico;
    private string? especialidade;
    private string? paciente;
    private string? data;
    private string? horario;

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

    public CancelarConsulta()
    {
        InitializeComponent();
    }

    // CONFIRMAR CANCELAMENTO
    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar = await DisplayAlertAsync(
            "Cancelar consulta",
            "Deseja realmente cancelar esta consulta?",
            "SIM",
            "NÃO");

        if (!confirmar)
            return;

        await DisplayAlertAsync(
            "Consulta cancelada",
            "A consulta foi cancelada com sucesso.",
            "OK");

        await Shell.Current.GoToAsync(
            nameof(MinhasConsultas));
    }

    // VOLTAR
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}