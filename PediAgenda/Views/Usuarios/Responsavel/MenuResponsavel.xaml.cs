namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class MenuResponsavel : ContentPage
{
    public MenuResponsavel()
    {
        InitializeComponent();
    }

    private async void PacientesButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "Pacientes",
            "A tela de pacientes ainda será implementada.",
            "OK");
    }

    private async void NotificacoesButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "Notificações",
            "A tela de notificações ainda será implementada.",
            "OK");
    }

    private async void MinhasConsultasButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "Minhas Consultas",
            "A tela de consultas ainda será implementada.",
            "OK");
    }

    private async void AgendarConsultaButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AgendarConsulta));
    }
}