using System.Text.RegularExpressions;

namespace PediAgenda.Views.Cadastro;

public partial class CadastroCampo : ContentPage
{
    private bool senhaVisivel = false;
    private bool confirmarSenhaVisivel = false;

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
        confirmarSenhaVisivel = !confirmarSenhaVisivel;

        ConfirmarSenhaEntry.IsPassword = !confirmarSenhaVisivel;

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

        //Validação do nome
        if (string.IsNullOrWhiteSpace(nome))
        {
            ExibirErro("Informe seu Nome.");
            return;
        }

        //Validação do CPF
        if (!ValidarCpf(cpf))
        {
            ExibirErro("Informe um CPF válido");
            return;
        }

        //Validação do E-mail
        if (!ValidarEmail(email))
        {
            ExibirErro("Informe um e-mail válido.");
            return;
        }

        // Validação da Senha
        if (senha.Length < 8)
        {
            ExibirErro("A senha deve possuir no mínimo 8 caracteres.");
            return;
        }

        // Confirmação da senha
        if (senha != confirmarSenha)
            {
                ExibirErro("As senhas não coincidem.");
                return;
            }

        // Cadastro validado com sucesso, exibe mensagem e navega para a próxima página
        await DisplayAlertAsync(
            "Excelente!",
            "Cadastro efetuado com sucesso.",
            "OK");

        await Shell.Current.GoToAsync(nameof(CadastroConcluido));
    }

    //Método validação do CPF
    private bool ValidarCpf(string cpf)
    {
        //Remove pontos, traços e outros caracteres que não sejam numeros
        cpf = Regex.Replace(cpf, @"\D", "");

        //Verificar se o CPF possui 11 numeros
        if (cpf.Length !=11)
        {
            return false;
        }

        //Verifica se todos os numeros são iguais. Ex.: 111111111
        if (cpf.All(c => c == cpf[0]))
        {
            return false;
        }
        //Se passou pelas validações acima, considera formato válido
            return true;
    }

    //Método validação do E-mail
    private bool ValidarEmail(string email)
    {
        //Verifica se o e-mail está vazio
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        //Verifica se o e-mail possui um formato valido
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
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