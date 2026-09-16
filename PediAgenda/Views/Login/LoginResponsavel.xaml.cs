namespace PediAgenda.Views.Login;

public partial class LoginResponsavel : ContentPage
{
    private bool senhaVisivel = false;

    public LoginResponsavel()
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

    // Botão de cadastro
    private async void CadastroButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(Cadastro.CadastroCampo));
    }

    //Botão de login, Precisa das validação dos campos e exibição de mensagens de erro
    private async void EntrarButton_Clicked(object sender, EventArgs e)
    {
        //Alerta Provisorio
        await DisplayAlertAsync(
            "Botão de Entrar",
            "Funções do botão de entrar ainda serão implementadas.",
            "OK");
    }

    // Método auxiliar para exibir mensagens de erro
    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }
}

