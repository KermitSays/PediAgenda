using PediAgenda.Views.Usuarios.Responsavel;
using System.Net.Http.Json;
using System.Text.Json;

namespace PediAgenda.Views.Login;

public partial class LoginResponsavel : ContentPage
{
    private bool senhaVisivel = false;
    private bool entrando = false;
    private static readonly HttpClient http = new()
    {
        BaseAddress = new Uri(
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:5000/"
                : "http://localhost:5000/"),
        Timeout = TimeSpan.FromSeconds(20)
    };

    public LoginResponsavel()
    {
        InitializeComponent();
    }

    //Exibe a mensagem  de erro na tela
    private void MostrarErro(string mensagem)
    {
        MensagemErroLabel.Text = mensagem;
        MensagemErroLabel.IsVisible = true;
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

   //Botão de login, Precisa das validação dos campos e exibição de mensagens de erro
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

            string conteudo = await resposta.Content.ReadAsStringAsync();

            // Erro interno da API pode não retornar JSON.
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
                        ? campo.GetString() ?? "Não foi possível entrar."
                        : "Não foi possível entrar.";

                MostrarErro(mensagem);
                return;
            }

            string perfil = dados.GetProperty("perfil").GetString() ?? "";
            string token = dados.GetProperty("token").GetString() ?? "";

            if (perfil != "RESPONSAVEL")
            {
                MostrarErro(
                    "Esta tela é para responsáveis. Use o login de funcionários.");
                return;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                MostrarErro("A API não retornou uma sessão válida.");
                return;
            }

            await SecureStorage.Default.SetAsync(
                "pediagenda_token", token);

            SenhaEntry.Text = "";

            await Shell.Current.GoToAsync(nameof(MenuResponsavel));
        }
        catch (HttpRequestException)
        {
            MostrarErro(
                "Não foi possível acessar a API. Verifique se ela está em execução.");
        }
        catch (TaskCanceledException)
        {
            MostrarErro(
                "A conexão demorou demais. Tente novamente.");
        }
        catch (JsonException)
        {
            MostrarErro("A API retornou uma resposta inesperada.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            MostrarErro(
                "Não foi possível concluir o login. Confira a janela Saída do Visual Studio.");
        }
        finally
        {
            entrando = false;
            EntrarButton.IsEnabled = true;
        }
    }
}

