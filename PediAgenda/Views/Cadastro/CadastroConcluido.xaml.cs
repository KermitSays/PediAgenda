
namespace PediAgenda.Views.Cadastro;

public partial class CadastroConcluido : ContentPage
{
    public CadastroConcluido()
    {
        InitializeComponent();
    }

    private async void VoltarLoginButton_Clicked(
        object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("../..");
    }
}
