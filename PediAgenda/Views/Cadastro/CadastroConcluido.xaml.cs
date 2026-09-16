using PediAgenda.Views;
using PediAgenda.Views.Login;

namespace PediAgenda.Views.Cadastro;

public partial class CadastroConcluido : ContentPage
{
    public CadastroConcluido()
    {
        InitializeComponent();
    }

    // Botão para voltar para a tela de login (responsavel)
    private async void VoltarLoginButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LoginResponsavel));
    }
}