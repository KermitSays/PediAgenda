namespace PediAgenda.Views;

public partial class SobreDesenvolvedores : ContentPage
{
    public SobreDesenvolvedores()
    {
        InitializeComponent();
    }

    private async void Flaticon_Tapped(object sender, TappedEventArgs e)
    {
        await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync("https://www.flaticon.com/");
    }
}
