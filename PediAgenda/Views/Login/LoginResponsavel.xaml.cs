using System.Net.Http.Json;
using System.Text.Json;
using PediAgenda.Dados;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Login;

public partial class LoginResponsavel : ContentPage
{
    private bool senhaVisivel =
        false;

    private bool entrando =
        false;


    private static readonly HttpClient http =
        new()
        {
            BaseAddress =
                new Uri(
                    "https://pediagenda-api.onrender.com/"),

            Timeout =
                TimeSpan.FromSeconds(90)
        };


    public LoginResponsavel()
    {
        InitializeComponent();
    }


    // =============================================
    // ERRO
    // =============================================

    private void MostrarErro(
        string mensagem)
    {
        MensagemErroLabel.Text =
            mensagem;


        MensagemErroLabel.IsVisible =
            true;
    }


    // =============================================
    // MOSTRAR / OCULTAR SENHA
    // =============================================

    private void MostrarSenhaButton_Clicked(
        object sender,
        EventArgs e)
    {
        senhaVisivel =
            !senhaVisivel;


        SenhaEntry.IsPassword =
            !senhaVisivel;


        MostrarSenhaButton.Source =
            SenhaEntry.IsPassword

                ? "olho_fechado.png"

                : "olho_aberto.png";
    }


    // =============================================
    // RECUPERAÇÃO DE SENHA
    // =============================================

    private async void EsqueciSenhaButton_Clicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlertAsync(
            "Recuperação de senha",
            "A recuperação de senha será implementada posteriormente.",
            "OK");
    }


    // =============================================
    // LOGIN
    // =============================================

    private async void EntrarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (entrando)
            return;


        MensagemErroLabel.IsVisible =
            false;


        string email =
            EmailEntry.Text?.Trim()
            ?? string.Empty;


        string senha =
            SenhaEntry.Text
            ?? string.Empty;


        if (
            string.IsNullOrWhiteSpace(email) ||

            string.IsNullOrWhiteSpace(senha))
        {
            MostrarErro(
                "Informe seu e-mail e sua senha.");


            return;
        }


        entrando =
            true;


        EntrarButton.IsEnabled =
            false;


        try
        {
            using var resposta =
                await http.PostAsJsonAsync(
                    "api/auth/login",
                    new
                    {
                        email,
                        senha
                    });


            string conteudo =
                await resposta.Content
                    .ReadAsStringAsync();


            if (
                (int)resposta.StatusCode >=
                500)
            {
                MostrarErro(
                    "A API apresentou um erro. " +
                    "Tente novamente em alguns instantes.");


                return;
            }


            using var documento =
                JsonDocument.Parse(
                    conteudo);


            JsonElement dados =
                documento.RootElement;


            if (!resposta.IsSuccessStatusCode)
            {
                string mensagem =
                    dados.TryGetProperty(
                        "mensagem",
                        out var campo)

                        ? campo.GetString()
                            ?? "Não foi possível entrar."

                        : "Não foi possível entrar.";


                MostrarErro(
                    mensagem);


                return;
            }


            string perfil =
                dados.TryGetProperty(
                    "perfil",
                    out var perfilCampo)

                &&

                perfilCampo.ValueKind ==
                    JsonValueKind.String

                    ? perfilCampo.GetString()
                        ?? string.Empty

                    : string.Empty;


            string token =
                dados.TryGetProperty(
                    "token",
                    out var tokenCampo)

                &&

                tokenCampo.ValueKind ==
                    JsonValueKind.String

                    ? tokenCampo.GetString()
                        ?? string.Empty

                    : string.Empty;


            int idUsuario =
                dados.TryGetProperty(
                    "id",
                    out var idCampo)

                &&

                idCampo.TryGetInt32(
                    out int id)

                    ? id

                    : 0;


            string nome =
                dados.TryGetProperty(
                    "nome",
                    out var nomeCampo)

                &&

                nomeCampo.ValueKind ==
                    JsonValueKind.String

                    ? nomeCampo.GetString()
                        ?? string.Empty

                    : string.Empty;


            string emailUsuario =
                dados.TryGetProperty(
                    "email",
                    out var emailCampo)

                &&

                emailCampo.ValueKind ==
                    JsonValueKind.String

                    ? emailCampo.GetString()
                        ?? string.Empty

                    : email;


            if (
                perfil !=
                "RESPONSAVEL")
            {
                MostrarErro(
                    "Esta tela é para responsáveis. " +
                    "Use o login de funcionários.");


                return;
            }


            if (string.IsNullOrWhiteSpace(token))
            {
                MostrarErro(
                    "A API não retornou uma sessão válida.");


                return;
            }


            // LIMPA POSSÍVEIS DADOS DA CONTA ANTERIOR

            ResponsavelDados.Pacientes
                .Clear();


            // GUARDA TOKEN

            await SecureStorage.Default
                .SetAsync(
                    "pediagenda_token",
                    token);


            // GUARDA A SESSÃO

            SessaoUsuario.Iniciar(
                idUsuario,
                nome,
                emailUsuario,
                perfil);


            SenhaEntry.Text =
                string.Empty;


            await Shell.Current.GoToAsync(
                nameof(MenuResponsavel));
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug
                .WriteLine(ex);


            MostrarErro(
                "Não foi possível acessar a API. " +
                "Verifique sua conexão e tente novamente.");
        }
        catch (TaskCanceledException ex)
        {
            System.Diagnostics.Debug
                .WriteLine(ex);


            MostrarErro(
                "A conexão demorou demais. Tente novamente.");
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug
                .WriteLine(ex);


            MostrarErro(
                "A API retornou uma resposta inesperada.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug
                .WriteLine(ex);


            MostrarErro(
                "Não foi possível concluir o login. " +
                "Confira a janela Saída do Visual Studio.");
        }
        finally
        {
            entrando =
                false;


            EntrarButton.IsEnabled =
                true;
        }
    }


    // =============================================
    // ACESSO RÁPIDO
    // =============================================

    private async void EntrarDiretoButton_Clicked(
        object sender,
        EventArgs e)
    {
        SecureStorage.Default.Remove(
            "pediagenda_token");


        SessaoUsuario.Iniciar(
            0,
            "Responsável Teste",
            string.Empty,
            "RESPONSAVEL");


        await Shell.Current.GoToAsync(
            nameof(MenuResponsavel));
    }
}