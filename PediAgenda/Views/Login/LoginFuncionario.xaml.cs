namespace PediAgenda.Views.Login;

public partial class LoginFuncionario : ContentPage
{
    private bool senhaVisivel = false;

    public LoginFuncionario()
    {
        InitializeComponent();
    }

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