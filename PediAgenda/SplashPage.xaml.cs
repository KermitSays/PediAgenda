
namespace PediAgenda;

public partial class SplashPage : ContentPage
{
    private bool _iniciada = false;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_iniciada)
            return;

        _iniciada = true;

        // Exibe a Splash por 1,8 segundo
        await Task.Delay(1800);

        // Abre o aplicativo normalmente
        if (Window != null && Window.Page == this)
        {
            Window.Page = new AppShell();
        }
    }
}
