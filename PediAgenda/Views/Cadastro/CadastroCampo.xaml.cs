using System.Net.Http.Headers;
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
        BaseAddress = new Uri("https://pediagenda-api.onrender.com/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public CadastroCampo()
    {
        InitializeComponent();
    }

    // Mostra ou oculta a senha
    private void MostrarSenhaButton_Clicked(object sender, EventArgs e)
    {
        senhaVisivel = !senhaVisivel;
        SenhaEntry.IsPassword = !senhaVisivel;

        MostrarSenhaButton.Source = SenhaEntry.IsPassword
            ? "olho_fechado.png"
            : "olho_aberto.png";
    }

    // Mostra ou oculta a confirmação da senha
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

    // Valida os campos e envia o cadastro à API
    private async void ContinuarButton_Clicked(object sender, EventArgs e)
    {
        if (cadastrando)
            return;

        MensagemErroLabel.IsVisible = false;

        string nome = NomeEntry.Text?.Trim() ?? "";
        string cpf = Regex.Replace(CpfEntry.Text ?? "", @"\D", "");
        string email = EmailEntry.Text?.Trim() ?? "";
        string telefone = Regex.Replace(
            TelefoneEntry.Text ?? "", @"\D", "");
        string senha = SenhaEntry.Text ?? "";
        string confirmarSenha = ConfirmarSenhaEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(nome) || nome.Length > 120)
        {
            ExibirErro("Informe um nome com até 120 caracteres.");
            return;
        }

        if (!ValidarCpf(cpf))
        {
            ExibirErro("Informe um CPF com 11 dígitos, sem repetição.");
            return;
        }

        if (!ValidarEmail(email) || email.Length > 160)
        {
            ExibirErro("Informe um e-mail válido com até 160 caracteres.");
            return;
        }

        if (telefone.Length != 10 && telefone.Length != 11)
        {
            ExibirErro("Informe o telefone com DDD, com 10 ou 11 dígitos.");
            return;
        }

        if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
        {
            ExibirErro("A senha deve possuir no mínimo 8 caracteres.");
            return;
        }

        if (senha != confirmarSenha)
        {
            ExibirErro("As senhas não coincidem.");
            return;
        }

        if (!TermosCheckBox.IsChecked)
        {
            ExibirErro("Você precisa aceitar os Termos de Uso.");
            return;
        }

        cadastrando = true;
        ContinuarButton.IsEnabled = false;
        VoltarButton.IsEnabled = false;

        bool cadastroGravado = false;

        try
        {
            string? token = await SecureStorage.Default.GetAsync(
                "pediagenda_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                ExibirErro(
                    "Faça login com a conta da recepção para cadastrar responsáveis.");
                return;
            }

            using var requisicao = new HttpRequestMessage(
                HttpMethod.Post,
                "api/cadastro/responsavel");

            requisicao.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            requisicao.Content = JsonContent.Create(new
            {
                nome,
                cpf,
                email,
                telefone,
                senha,
                aceiteTermos = TermosCheckBox.IsChecked
            });

            using var resposta = await http.SendAsync(requisicao);

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            if ((int)resposta.StatusCode >= 500)
            {
                ExibirErro(
                    "A API apresentou um erro. Confira o terminal da API.");
                return;
            }

            if (!resposta.IsSuccessStatusCode)
            {
                using var documento = JsonDocument.Parse(conteudo);
                var dados = documento.RootElement;

                string mensagem =
                    dados.TryGetProperty("mensagem", out var campo)
                        ? campo.GetString() ?? "Não foi possível cadastrar."
                        : "Não foi possível cadastrar.";

                // Mostra os detalhes de validação enviados pela API.
                if (dados.TryGetProperty("campos", out var campos) &&
                    campos.ValueKind == JsonValueKind.Object)
                {
                    var detalhes = campos.EnumerateObject()
                        .Select(item => item.Value.GetString() ?? "")
                        .Where(texto => !string.IsNullOrWhiteSpace(texto));

                    mensagem += "\n" + string.Join("\n", detalhes);
                }

                ExibirErro(mensagem);
                return;
            }

            cadastroGravado = true;

            SenhaEntry.Text = "";
            ConfirmarSenhaEntry.Text = "";

            await DisplayAlertAsync(
                "Cadastro concluído",
                "Responsável cadastrado com sucesso.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ExibirErro(
                "Não foi possível confirmar o cadastro. Verifique a conexão " +
                "e tente entrar com o e-mail informado antes de cadastrar novamente.");
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ExibirErro(
                "A resposta demorou demais. Tente entrar com o e-mail informado " +
                "para verificar se o cadastro foi concluído.");
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ExibirErro("A API retornou uma resposta inesperada.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ExibirErro(cadastroGravado
                ? "O cadastro foi gravado, mas não foi possível abrir a próxima tela. " +
                  "Volte ao login para entrar."
                : "Não foi possível concluir o cadastro. Confira a janela Saída.");
        }
        finally
        {
            cadastrando = false;
            ContinuarButton.IsEnabled = !cadastroGravado;
            VoltarButton.IsEnabled = true;
        }
    }

    // Validação simplificada: formato e dígitos repetidos.
    // Não verifica os dígitos verificadores do CPF.
    private bool ValidarCpf(string cpf)
    {
        cpf = Regex.Replace(cpf, @"\D", "");

        return cpf.Length == 11 &&
               !cpf.All(c => c == cpf[0]);
    }

    private bool ValidarEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) &&
               Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private void ExibirErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }

    private async void VoltarButton_Clicked(object sender, EventArgs e)
    {
        if (cadastrando)
            return;

        await Shell.Current.GoToAsync("..");
    }
}