namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
[QueryProperty(nameof(TipoAtendimento), "TipoAtendimento")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
public partial class AgendarConfirmacao : ContentPage
{
    private string? medico;
    private string? especialidade;
    private string? data;
    private string? horario;
    private string? tipoAtendimento;
    private string? fotoMedico;


    // MÉDICO

    public string Medico
    {
        get => medico ?? string.Empty;

        set
        {
            medico = value;

            if (MedicoLabel != null)
            {
                MedicoLabel.Text = value;
            }
        }
    }

    // FOTO DO MÉDICO
    public string FotoMedico
    {
        get => fotoMedico ?? string.Empty;
        set => fotoMedico = value;
    }


    // ESPECIALIDADE

    public string Especialidade
    {
        get => especialidade ?? string.Empty;

        set
        {
            especialidade = value;

            if (EspecialidadeLabel != null)
            {
                EspecialidadeLabel.Text = value;
            }
        }
    }


    // DATA

    public string Data
    {
        get => data ?? string.Empty;

        set
        {
            data = value;

            if (DataLabel != null)
            {
                DataLabel.Text = value;
            }
        }
    }


    // HORÁRIO

    public string Horario
    {
        get => horario ?? string.Empty;

        set
        {
            horario = value;

            if (HorarioLabel != null)
            {
                HorarioLabel.Text = value;
            }
        }
    }


    // TIPO DE ATENDIMENTO

    public string TipoAtendimento
    {
        get => tipoAtendimento ?? string.Empty;

        set
        {
            tipoAtendimento = value;

            if (TipoAtendimentoLabel != null)
            {
                TipoAtendimentoLabel.Text = value;
            }
        }
    }


    public AgendarConfirmacao()
    {
        InitializeComponent();
    }


    // CONFIRMAR AGENDAMENTO

    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(AgendamentoConcluido),
            new Dictionary<string, object>
            {
            { "Medico", Medico },
            { "FotoMedico", FotoMedico },
            { "Especialidade", Especialidade },
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