using System.Text.RegularExpressions;

namespace PediAgenda.Views.Cadastro;

public partial class CadastroCampo : ContentPage
{
    public CadastroCampo()
    {
        InitializeComponent();
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