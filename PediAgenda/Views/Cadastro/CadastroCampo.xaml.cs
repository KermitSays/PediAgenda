
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PediAgenda.Views.Cadastro;

public partial class CadastroCampo : ContentPage
{
    private bool senhaVisivel = false;
    private bool confirmarSenhaVisivel = false;
    private bool cadastrando = false;

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri(
            "https://pediagenda-api.onrender.com/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public CadastroCampo()
    {
        InitializeComponent();
    }

    // Mostrar ou ocultar senha
    private void MostrarSenhaButton_Clicked(
        object sender, EventArgs e)
    {
        senhaVisivel = !senhaVisivel;
        SenhaEntry.IsPassword = !senhaVisivel;

        MostrarSenhaButton.Source = SenhaEntry.IsPassword
            ? "olho_fechado.png"
            : "olho_aberto.png";
    }

    // Mostrar ou ocultar confirmação da senha
    private void MostrarConfirmarSenhaButton_Clicked(
        object sender, EventArgs e)
    {
        confirmarSenhaVisivel = !confirmarSenhaVisivel;
        ConfirmarSenhaEntry.IsPassword = !confirmarSenhaVisivel;

        MostrarConfirmarSenhaButton.Source =
            ConfirmarSenhaEntry.IsPassword
                ? "olho_fechado.png"
                : "olho_aberto.png";
    }

    // Validação e envio do cadastro
    private async void ContinuarButton_Clicked(
        object sender, EventArgs e)
    {
        if (cadastrando)
            return;

        MensagemErroLabel.IsVisible = false;

        string cpf = Regex.Replace(
            CpfEntry.Text ?? "", @"\D", "");

        string codigoAtivacao =
            CodigoAtivacaoEntry.Text?.Trim() ?? "";

        string email =
            EmailEntry.Text?.Trim() ?? "";

        string senha =
            SenhaEntry.Text ?? "";

        string confirmarSenha =
            ConfirmarSenhaEntry.Text ?? "";

        // Validação do CPF
        if (!ValidarCpf(cpf))
        {
            ExibirErro(
                "Informe um CPF com 11 dígitos, sem repetição.");
            return;
        }

        // Validação do código de ativação
        if (string.IsNullOrWhiteSpace(codigoAtivacao))
        {
            ExibirErro(
                "Informe o código de ativação fornecido pela clínica.");
            return;
        }

        // Validação do e-mail
        if (!ValidarEmail(email) || email.Length > 160)
        {
            ExibirErro(
                "Informe um e-mail válido com até 160 caracteres.");
            return;
        }

        // Validação da senha
        if (string.IsNullOrWhiteSpace(senha) ||
            senha.Length < 8)
        {
            ExibirErro(
                "A senha deve possuir no mínimo 8 caracteres.");
            return;
        }

        // Confirmação da senha
        if (senha != confirmarSenha)
        {
            ExibirErro("As senhas não coincidem.");
            return;
        }

        // Termos de uso
        if (!TermosCheckBox.IsChecked)
        {
            ExibirErro(
                "Você precisa aceitar os Termos de Uso.");
            return;
        }

        cadastrando = true;
        ContinuarButton.IsEnabled = false;
        VoltarButton.IsEnabled = false;

        try
        {
            // Envia os dados para a API
            // O responsável já existe no banco da clínica.
            // Apenas as credenciais de acesso serão cadastradas.
            using var resposta = await http.PostAsJsonAsync(
                "api/cadastro/responsavel",
                new
                {
                    cpf,
                    codigoAtivacao,
                    email,
                    senha,
                    aceiteTermos = TermosCheckBox.IsChecked
                });

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            // Tratamento de erros da API
            if (!resposta.IsSuccessStatusCode)
            {
                string mensagem =
                    "Não foi possível iniciar o cadastro.";

                if ((int)resposta.StatusCode >= 500)
                {
                    mensagem =
                        "O servidor apresentou um problema. " +
                        "Tente novamente em alguns instantes.";
                }
                else if (!string.IsNullOrWhiteSpace(conteudo))
                {
                    using var documento =
                        JsonDocument.Parse(conteudo);

                    var dados = documento.RootElement;

                    if (dados.TryGetProperty(
                        "mensagem", out var campo))
                    {
                        mensagem =
                            campo.GetString() ?? mensagem;
                    }

                    if (dados.TryGetProperty(
                        "campos", out var campos) &&
                        campos.ValueKind ==
                            JsonValueKind.Object)
                    {
                        var detalhes =
                            campos.EnumerateObject()
                                .Select(item =>
                                    item.Value.GetString() ?? "")
                                .Where(texto =>
                                    !string.IsNullOrWhiteSpace(texto));

                        var textos = detalhes.ToArray();

                        if (textos.Length > 0)
                        {
                            mensagem += "\n" +
                                string.Join("\n", textos);
                        }
                    }
                }

                ExibirErro(mensagem);
                return;
            }

            // Cadastro iniciado com sucesso.
            // A conta ainda aguarda confirmação do e-mail.
            SenhaEntry.Text = "";
            ConfirmarSenhaEntry.Text = "";
            CodigoAtivacaoEntry.Text = "";

            await DisplayAlertAsync(
                "Verifique seu e-mail",
                "Enviamos um link de confirmação para " +
                email + ".\n\n" +
                "Acesse sua caixa de entrada e clique " +
                "no link para ativar sua conta.\n\n" +
                "Depois da confirmação, você poderá " +
                "entrar no PediAgenda.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ExibirErro(
                "Não foi possível comunicar com a API. " +
                "Verifique sua conexão. Se o cadastro " +
                "já tiver sido iniciado, não será " +
                "necessário criar outro usuário.");
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ExibirErro(
                "A API demorou para responder. " +
                "Verifique sua conexão e tente novamente.");
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ExibirErro(
                "A API retornou uma resposta inesperada.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ExibirErro(
                "Não foi possível concluir a operação. " +
                "Confira a janela Saída do Visual Studio.");
        }
        finally
        {
            cadastrando = false;
            ContinuarButton.IsEnabled = true;
            VoltarButton.IsEnabled = true;
        }
    }

    // Validação simplificada de CPF
    private static bool ValidarCpf(string cpf)
    {
        return cpf.Length == 11 &&
               !cpf.All(c => c == cpf[0]);
    }

    // Validação de e-mail
    private static bool ValidarEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
               Regex.IsMatch(
                   email,
                   @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    // Exibir mensagens de erro
    private void ExibirErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }

    // Voltar à tela anterior
    private async void VoltarButton_Clicked(
        object sender, EventArgs e)
    {
        if (cadastrando)
            return;

        await Shell.Current.GoToAsync("..");
    }
}
