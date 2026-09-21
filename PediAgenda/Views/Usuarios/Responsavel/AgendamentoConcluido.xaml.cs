namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
public partial class AgendamentoConcluido : ContentPage
{
    private string? medico;
    private string? fotoMedico;
    private string? especialidade;
    private string? data;
    private string? horario;

    public string Medico
    {
        get => medico ?? string.Empty;
        set
        {
            medico = value;
            AtualizarResumo();
        }
    }

    public string FotoMedico
    {
        get => fotoMedico ?? string.Empty;
        set
        {
            fotoMedico = value;
            AtualizarResumo();
        }
    }

    public string Especialidade
    {
        get => especialidade ?? string.Empty;
        set
        {
            especialidade = value;
            AtualizarResumo();
        }
    }

    public string Data
    {
        get => data ?? string.Empty;
        set
        {
            data = value;
            AtualizarResumo();
        }
    }

    public string Horario
    {
        get => horario ?? string.Empty;
        set
        {
            horario = value;
            AtualizarResumo();
        }
    }

    public AgendamentoConcluido()
    {
        InitializeComponent();

        BindingContext = this;
    }

    // ATUALIZA O RESUMO

    private void AtualizarResumo()
    {
        OnPropertyChanged(nameof(Medico));
        OnPropertyChanged(nameof(FotoMedico));
        OnPropertyChanged(nameof(Especialidade));
        OnPropertyChanged(nameof(Data));
        OnPropertyChanged(nameof(Horario));
    }

    // MINHAS CONSULTAS

    private async void ConsultasButton_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlertAsync(
            "Minhas consultas",
            "A tela de consultas será disponibilizada em seguida.",
            "OK");
    }

    // VOLTAR AO MENU

    private async void MenuButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MenuResponsavel));
    }
}