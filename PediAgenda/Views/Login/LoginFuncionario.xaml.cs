using System.Net.Http.Json;
using System.Text.Json;
using PediAgenda.Views.Usuarios.Medico;
using PediAgenda.Views.Usuarios.Recepcao;

namespace PediAgenda.Views.Login;

public partial class LoginFuncionario : ContentPage
{
    private bool senhaVisivel = false;
    private bool entrando = false;

    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri("https://pediagenda-api.onrender.com/"),
        Timeout = TimeSpan.FromSeconds(90)
    };

    public LoginFuncionario()
    {
        InitializeComponent();
    }

    // Mostra ou oculta a senha.
    private void MostrarSenhaButton_Clicked(object sender, EventArgs e)
    {
        senhaVisivel = !senhaVisivel;

        SenhaEntry.IsPassword = !senhaVisivel;

        MostrarSenhaButton.Source = SenhaEntry.IsPassword
            ? "olho_fechado.png"
            : "olho_aberto.png";
    }

    // Recuperação de senha.
    private async void EsqueciSenhaButton_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlertAsync(
            "Recuperação de senha",
            "A recuperação de senha será implementada posteriormente.",
            "OK");
    }

    // Valida o login na API e abre o menu do perfil.
    private async void EntrarButton_Clicked(object sender, EventArgs e)
    {
        if (entrando)
            return;

        MensagemErroLabel.IsVisible = false;

        string email = EmailEntry.Text?.Trim() ?? "";
        string senha = SenhaEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha))
        {
            MostrarErro("Informe seu e-mail e sua senha.");
            return;
        }

        entrando = true;
        EntrarButton.IsEnabled = false;

        try
        {
            using var resposta = await http.PostAsJsonAsync(
                "api/auth/login",
                new { email, senha });

            string conteudo =
                await resposta.Content.ReadAsStringAsync();

            if ((int)resposta.StatusCode >= 500)
            {
                MostrarErro(
                    "A API apresentou um erro. Confira o terminal da API.");
                return;
            }

            using var documento = JsonDocument.Parse(conteudo);
            var dados = documento.RootElement;

            if (!resposta.IsSuccessStatusCode)
            {
                string mensagem =
                    dados.TryGetProperty("mensagem", out var campo)
                    && campo.ValueKind == JsonValueKind.String
                        ? campo.GetString() ?? "Não foi possível entrar."
                        : "Não foi possível entrar.";

                MostrarErro(mensagem);
                return;
            }

            if (!dados.TryGetProperty("perfil", out var perfilCampo) ||
                perfilCampo.ValueKind != JsonValueKind.String ||
                !dados.TryGetProperty("token", out var tokenCampo) ||
                tokenCampo.ValueKind != JsonValueKind.String)
            {
                MostrarErro("A API retornou uma resposta inesperada.");
                return;
            }

            string perfil = perfilCampo.GetString() ?? "";
            string token = tokenCampo.GetString() ?? "";

            string rota;

            switch (perfil)
            {
                case "MEDICO":
                    rota = nameof(MenuMedico);
                    break;

                case "RECEPCIONISTA":
                    rota = nameof(MenuRecepcao);
                    break;

                default:
                    MostrarErro(
                        "Esta tela é para funcionários. " +
                        "Responsáveis devem usar o acesso de responsável.");
                    return;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                MostrarErro("A API não retornou uma sessão válida.");
                return;
            }

            // Remove os dados locais da conta anterior.
            PediAgenda.Views.Usuarios.Responsavel
                .ResponsavelDados.Pacientes.Clear();

            await SecureStorage.Default.SetAsync(
                "pediagenda_token",
                token);

            SenhaEntry.Text = "";

            await Shell.Current.GoToAsync(rota);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            MostrarErro(
                "Não foi possível acessar a API. " +
                "Verifique se ela está em execução.");
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            MostrarErro(
                "A conexão demorou demais. Tente novamente.");
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            MostrarErro("A API retornou uma resposta inesperada.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            MostrarErro(
                "Não foi possível concluir o login. " +
                "Confira a janela Saída do Visual Studio.");
        }
        finally
        {
            entrando = false;
            EntrarButton.IsEnabled = true;
        }
    }

    // Exibe a mensagem de erro.
    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
    }

    // Botões de acesso rápido para entrar como médico ou recepcionista sem precisar digitar e-mail e senha.
    private async void EntrarMedicoButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Usuarios.Medico.MenuMedico));
    }

    private async void EntrarRecepcionistaButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(Usuarios.Recepcao.MenuRecepcao));
    }
}