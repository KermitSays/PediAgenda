namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(TipoConsulta), "TipoConsulta")]
[QueryProperty(nameof(DataConsulta), "DataConsulta")]
[QueryProperty(nameof(Observacoes), "Observacoes")]
public partial class DetalhesProntuario : ContentPage
{
    private string tipoConsulta = string.Empty;
    private string dataConsulta = string.Empty;
    private string observacoes = string.Empty;

    public string TipoConsulta
    {
        get => tipoConsulta;

        set
        {
            tipoConsulta = value;

            if (TipoConsultaLabel != null)
            {
                TipoConsultaLabel.Text = value;
            }
        }
    }

    public string DataConsulta
    {
        get => dataConsulta;

        set
        {
            dataConsulta = value;

            if (DataConsultaLabel != null)
            {
                DataConsultaLabel.Text = value;
            }
        }
    }

    public string Observacoes
    {
        get => observacoes;

        set
        {
            observacoes = value;

            if (ObservacoesLabel != null)
            {
                ObservacoesLabel.Text = value;
            }
        }
    }

    public DetalhesProntuario()
    {
        InitializeComponent();
    }

    // Volta para o prontuário
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}