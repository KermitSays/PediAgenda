namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class MenuResponsavel : ContentPage
{
    public MenuResponsavel()
    {
        InitializeComponent();
    }

    private async void PacientesButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PacientesResponsavel));
    }

    private async void NotificacoesButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Notificacoes));
    }

    private async void MinhasConsultasButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MinhasConsultas));
    }

    private async void AgendarConsultaButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AgendarConsulta));
    }
}