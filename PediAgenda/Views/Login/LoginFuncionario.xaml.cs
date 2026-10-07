using System.Net.Http.Json;
using System.Text.Json;
using PediAgenda.Dados;
using PediAgenda.Views.Usuarios.Medico;
using PediAgenda.Views.Usuarios.Recepcao;

namespace PediAgenda.Views.Login;

public partial class LoginFuncionario : ContentPage
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


    public LoginFuncionario()
    {
        InitializeComponent();
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

                    &&

                    campo.ValueKind ==
                        JsonValueKind.String

                        ? campo.GetString()
                            ?? "Não foi possível entrar."

                        : "Não foi possível entrar.";


                MostrarErro(
                    mensagem);


                return;
            }


            if (
                !dados.TryGetProperty(
                    "perfil",
                    out var perfilCampo)

                ||

                perfilCampo.ValueKind !=
                    JsonValueKind.String

                ||

                !dados.TryGetProperty(
                    "token",
                    out var tokenCampo)

                ||

                tokenCampo.ValueKind !=
                    JsonValueKind.String)
            {
                MostrarErro(
                    "A API retornou uma resposta inesperada.");


                return;
            }


            string perfil =
                perfilCampo.GetString()
                ?? string.Empty;


            string token =
                tokenCampo.GetString()
                ?? string.Empty;


            // ID

            int idUsuario =
                dados.TryGetProperty(
                    "id",
                    out var idCampo)

                &&

                idCampo.TryGetInt32(
                    out int id)

                    ? id

                    : 0;


            // NOME

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


            // EMAIL

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


            string rota;


            switch (perfil)
            {
                case "MEDICO":

                    rota =
                        nameof(MenuMedico);

                    break;


                case "RECEPCIONISTA":

                    rota =
                        nameof(MenuRecepcao);

                    break;


                default:

                    MostrarErro(
                        "Esta tela é para funcionários. " +
                        "Responsáveis devem usar o acesso de responsável.");


                    return;
            }


            if (string.IsNullOrWhiteSpace(token))
            {
                MostrarErro(
                    "A API não retornou uma sessão válida.");


                return;
            }


            // LIMPA DADOS DA CONTA ANTERIOR

            PediAgenda
                .Views
                .Usuarios
                .Responsavel
                .ResponsavelDados
                .Pacientes
                .Clear();


            // GUARDA TOKEN

            await SecureStorage.Default
                .SetAsync(
                    "pediagenda_token",
                    token);


            // GUARDA DADOS DA SESSÃO

            SessaoUsuario.Iniciar(
                idUsuario,
                nome,
                emailUsuario,
                perfil);


            SenhaEntry.Text =
                string.Empty;


            await Shell.Current.GoToAsync(
                rota);
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
    // MENSAGEM DE ERRO
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
    // ACESSO RÁPIDO - MÉDICO
    // =============================================

    private async void EntrarMedicoButton_Clicked(
        object sender,
        EventArgs e)
    {
        SecureStorage.Default.Remove(
            "pediagenda_token");


        SessaoUsuario.Iniciar(
            0,
            "Médico Teste",
            string.Empty,
            "MEDICO");


        await Shell.Current.GoToAsync(
            nameof(MenuMedico));
    }


    // =============================================
    // ACESSO RÁPIDO - RECEPÇÃO
    // =============================================

    private async void EntrarRecepcionistaButton_Clicked(
        object sender,
        EventArgs e)
    {
        SecureStorage.Default.Remove(
            "pediagenda_token");


        SessaoUsuario.Iniciar(
            0,
            "Recepção Teste",
            string.Empty,
            "RECEPCIONISTA");


        await Shell.Current.GoToAsync(
            nameof(MenuRecepcao));
    }
}