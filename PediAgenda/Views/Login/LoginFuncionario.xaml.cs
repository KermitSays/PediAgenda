using PediAgenda.Views.Usuarios.Medico;

namespace PediAgenda.Views.Login;

public partial class LoginFuncionario : ContentPage
{
    private bool senhaVisivel = false;

    public LoginFuncionario()
    {
        InitializeComponent();
    }

    // Botão para mostrar ou ocultar a senha
    private void MostrarSenhaButton_Clicked(object sender, EventArgs e)
    {
        senhaVisivel = !senhaVisivel;

        SenhaEntry.IsPassword = !senhaVisivel;

        MostrarSenhaButton.Source = SenhaEntry.IsPassword
        ? "olho_fechado.png"
        : "olho_aberto.png";
    }

    // Botão para recuperação de senha
    private async void EsqueciSenhaButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "Recuperação de senha",
            "A recuperação de senha será implementada posteriormente.",
            "OK");
    }

    //Botão de login, Precisa da validação dos campos e exibição de mensagens de erro
    private async void EntrarButton_Clicked(object sender, EventArgs e)
    {
        //Alerta Provisorio
        await DisplayAlertAsync(
            "Botão de Entrar",
            "Botão ainda será implementado.",
            "OK");
    }

    private async void EntrarMedicoButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Usuarios.Medico.MenuMedico));
    }

    private async void EntrarRecepcionistaButton_Clicked(object sender, EventArgs e)
    {
        //Alerta Provisorio
        await DisplayAlertAsync(
            "Botão de Entrar",
            "Botão ainda será implementado.",
            "OK");
    }

    // Método auxiliar para exibir mensagens de erro
    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}