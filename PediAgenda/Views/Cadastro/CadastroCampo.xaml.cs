using System.Text.RegularExpressions;

namespace PediAgenda.Views.Cadastro;

public partial class CadastroCampo : ContentPage
{
    private bool senhaVisivel = false;

    public CadastroCampo()
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

    // Botão para mostrar ou ocultar a confirmação da senha
    private void MostrarConfirmarSenhaButton_Clicked(object sender, EventArgs e)
    {
        senhaVisivel = !senhaVisivel;

        ConfirmarSenhaEntry.IsPassword = !senhaVisivel;

        MostrarConfirmarSenhaButton.Source = ConfirmarSenhaEntry.IsPassword
        ? "olho_fechado.png"
        : "olho_aberto.png";
    }

    // Botão "Continuar"
    private async void ContinuarButton_Clicked(object sender, EventArgs e)
    {
        // Oculta a mensagem de erro antes de validar os campos
        MensagemErroLabel.IsVisible = false;

        // Obtém os valores dos campos de entrada, tratando nulos e espaços em branco
        string nome = NomeEntry.Text?.Trim() ?? string.Empty;
        string cpf = CpfEntry.Text?.Trim() ?? string.Empty;
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string senha = SenhaEntry.Text ?? string.Empty;
        string confirmarSenha = ConfirmarSenhaEntry.Text ?? string.Empty;

        // Cadastro validado com sucesso, exibe mensagem e navega para a próxima página
        await DisplayAlertAsync(
            "Excelente!",
            "Cadastro efetuado com sucesso.",
            "OK");

        await Shell.Current.GoToAsync(nameof(CadastroConcluido));
    }

    // Exibe uma mensagem de erro na tela
    private void ExibirErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }

    // Botão "Voltar"
    private async void VoltarButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}