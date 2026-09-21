namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
public partial class AgendarTipoAtendimento : ContentPage
{
    // Campos privados para armazenar os valores das propriedades
    private string? medico;
    private string? especialidade;
    private string? data;
    private string? horario;
    private string? tipoAtendimento;
    private string? convenioSelecionado;
    private string? fotoMedico;

    // Propriedades públicas para acessar os valores das propriedades
    public string Medico
    {
        get => medico ?? string.Empty;
        set => medico = value;
    }

    public string FotoMedico
    {
        get => fotoMedico ?? string.Empty;
        set => fotoMedico = value;
    }

    public string Especialidade
    {
        get => especialidade ?? string.Empty;
        set => especialidade = value;
    }

    public string Data
    {
        get => data ?? string.Empty;
        set => data = value;
    }

    public string Horario
    {
        get => horario ?? string.Empty;
        set => horario = value;
    }

    public AgendarTipoAtendimento()
    {
        InitializeComponent();
    }
      
    // Card de atendimento convênio selecionado
    private void ConvenioCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        tipoAtendimento = "Convênio";

        ConvenioCard.Stroke =
            (Color)Application.Current.Resources["PrimaryBlue"];

        ParticularCard.Stroke =
            (Color)Application.Current.Resources["InputBlue"];

        ConveniosContainer.IsVisible = true;

        ErroLabel.IsVisible = false;
    }

    // Card de atendimento particular selecionado
    private void ParticularCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        tipoAtendimento = "Particular";

        ParticularCard.Stroke =
            (Color)Application.Current.Resources["PrimaryBlue"];

        ConvenioCard.Stroke =
            (Color)Application.Current.Resources["InputBlue"];

        ConveniosContainer.IsVisible = false;

        convenioSelecionado = null;

        ErroLabel.IsVisible = false;
    }

    // Evento disparado quando o usuário seleciona um convênio na lista
    private void ConvenioPicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (ConvenioPicker.SelectedIndex >= 0)
        {
            convenioSelecionado =
                ConvenioPicker.Items[
                    ConvenioPicker.SelectedIndex];

            ErroLabel.IsVisible = false;
        }
        else
        {
            convenioSelecionado = null;
        }
    }

    // CONTINUAR

    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        // Verifica se o usuário escolheu entre Convênio e Particular.
        if (string.IsNullOrEmpty(tipoAtendimento))
        {
            ErroLabel.Text =
                "Selecione o tipo de atendimento.";

            ErroLabel.IsVisible = true;

            return;
        }

        // Se escolheu Convênio, verifica se também escolheu qual convênio.
        if (tipoAtendimento == "Convênio" &&
            string.IsNullOrEmpty(convenioSelecionado))
        {
            ErroLabel.Text =
                "Selecione o convênio.";

            ErroLabel.IsVisible = true;

            return;
        }
        
        string formaPagamento =
            tipoAtendimento == "Convênio"
                ? convenioSelecionado!
                : "Particular - R$ 200,00";


        await Shell.Current.GoToAsync(
            nameof(AgendarConfirmacao),
            new Dictionary<string, object>
            {
                { "Medico", Medico },
                { "FotoMedico", FotoMedico },
                { "Especialidade", Especialidade },
                { "Data", Data },
                { "Horario", Horario },
                { "TipoAtendimento", formaPagamento }
            });
    }

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}