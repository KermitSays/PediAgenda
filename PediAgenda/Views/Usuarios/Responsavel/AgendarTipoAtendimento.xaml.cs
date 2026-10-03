namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(MedicoId), "MedicoId")]
[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(HorarioId), "HorarioId")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
public partial class AgendarTipoAtendimento : ContentPage
{
    private int pacienteId;

    private int medicoId;

    private int horarioId;

    private string? paciente;

    private string? medico;

    private string? especialidade;

    private string? data;

    private string? horario;

    private string? tipoAtendimento;

    private string? convenioSelecionado;

    private string? fotoMedico;


    public int PacienteId
    {
        get => pacienteId;

        set => pacienteId = value;
    }


    public int MedicoId
    {
        get => medicoId;

        set => medicoId = value;
    }


    public int HorarioId
    {
        get => horarioId;

        set => horarioId = value;
    }


    public string Paciente
    {
        get => paciente ?? string.Empty;

        set => paciente = value;
    }


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


    // CONVÊNIO

    private void ConvenioCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        tipoAtendimento =
            "Convênio";


        ConvenioCard.Stroke =
            (Color)Application.Current!
                .Resources["PrimaryBlue"];


        ParticularCard.Stroke =
            (Color)Application.Current!
                .Resources["InputBlue"];


        ConveniosContainer.IsVisible =
            true;


        ErroLabel.IsVisible =
            false;
    }


    // PARTICULAR

    private void ParticularCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        tipoAtendimento =
            "Particular";


        ParticularCard.Stroke =
            (Color)Application.Current!
                .Resources["PrimaryBlue"];


        ConvenioCard.Stroke =
            (Color)Application.Current!
                .Resources["InputBlue"];


        ConveniosContainer.IsVisible =
            false;


        convenioSelecionado =
            null;


        ErroLabel.IsVisible =
            false;
    }


    // CONVÊNIO ESCOLHIDO

    private void ConvenioPicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (ConvenioPicker.SelectedIndex >= 0)
        {
            convenioSelecionado =
                ConvenioPicker.Items[
                    ConvenioPicker.SelectedIndex];


            ErroLabel.IsVisible =
                false;
        }
        else
        {
            convenioSelecionado =
                null;
        }
    }


    // CONTINUAR

    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrEmpty(
            tipoAtendimento))
        {
            ErroLabel.Text =
                "Selecione o tipo de atendimento.";


            ErroLabel.IsVisible =
                true;


            return;
        }


        if (tipoAtendimento == "Convênio" &&
            string.IsNullOrEmpty(
                convenioSelecionado))
        {
            ErroLabel.Text =
                "Selecione o convênio.";


            ErroLabel.IsVisible =
                true;


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
                {
                    "PacienteId",
                    PacienteId
                },

                {
                    "Paciente",
                    Paciente
                },

                {
                    "MedicoId",
                    MedicoId
                },

                {
                    "Medico",
                    Medico
                },

                {
                    "FotoMedico",
                    FotoMedico
                },

                {
                    "Especialidade",
                    Especialidade
                },

                {
                    "HorarioId",
                    HorarioId
                },

                {
                    "Data",
                    Data
                },

                {
                    "Horario",
                    Horario
                },

                {
                    "TipoAtendimento",
                    formaPagamento
                }
            });
    }


    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}