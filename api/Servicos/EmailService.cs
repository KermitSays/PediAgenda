using System.Net;
using System.Net.Http.Json;

namespace PediAgenda.Api.Servicos;

public class EmailService(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task EnviarConfirmacaoCadastroAsync(
        string emailDestino,
        string nomeDestino,
        string token)
    {
        var apiKey =
            Environment.GetEnvironmentVariable(
                "PEDIAGENDA_EMAIL_API_KEY");

        var emailRemetente =
            Environment.GetEnvironmentVariable(
                "PEDIAGENDA_EMAIL_REMETENTE");

        var nomeRemetente =
            Environment.GetEnvironmentVariable(
                "PEDIAGENDA_EMAIL_NOME");

        var urlPublica =
            Environment.GetEnvironmentVariable(
                "RENDER_EXTERNAL_URL");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "A variável PEDIAGENDA_EMAIL_API_KEY não foi configurada.");
        }

        if (string.IsNullOrWhiteSpace(emailRemetente))
        {
            throw new InvalidOperationException(
                "A variável PEDIAGENDA_EMAIL_REMETENTE não foi configurada.");
        }

        if (string.IsNullOrWhiteSpace(nomeRemetente))
        {
            nomeRemetente = "PediAgenda";
        }

        if (string.IsNullOrWhiteSpace(urlPublica))
        {
            throw new InvalidOperationException(
                "A URL pública da API não foi encontrada.");
        }

        var tokenUrl =
            Uri.EscapeDataString(token);

        var linkConfirmacao =
            $"{urlPublica.TrimEnd('/')}" +
            $"/api/cadastro/confirmar-email?token={tokenUrl}";

        var nomeSeguro =
            WebUtility.HtmlEncode(nomeDestino);

        var linkSeguro =
            WebUtility.HtmlEncode(linkConfirmacao);

        var html = $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
                <meta charset="UTF-8">
            </head>

            <body style="font-family: Arial, sans-serif; line-height: 1.6;">
                <h2>Confirme seu cadastro no PediAgenda</h2>

                <p>Olá, {nomeSeguro}!</p>

                <p>
                    Seu acesso ao PediAgenda está quase pronto.
                    Para confirmar seu e-mail e ativar sua conta,
                    clique no botão abaixo.
                </p>

                <p style="margin: 30px 0;">
                    <a
                        href="{linkSeguro}"
                        style="
                            background-color: #5B7CFA;
                            color: white;
                            padding: 12px 20px;
                            text-decoration: none;
                            border-radius: 6px;
                            display: inline-block;
                        ">
                        Confirmar meu e-mail
                    </a>
                </p>

                <p>
                    Este link é válido por 24 horas.
                </p>

                <p>
                    Se você não solicitou este cadastro,
                    pode ignorar esta mensagem.
                </p>

                <hr>

                <p style="font-size: 12px; color: #666;">
                    PediAgenda - Sistema de Agendamento e Atendimento Pediátrico
                </p>
            </body>
            </html>
            """;

        var requisicao = new
        {
            sender = new
            {
                name = nomeRemetente,
                email = emailRemetente
            },

            to = new[]
            {
                new
                {
                    email = emailDestino,
                    name = nomeDestino
                }
            },

            subject =
                "Confirme seu cadastro no PediAgenda",

            htmlContent =
                html
        };

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.brevo.com/v3/smtp/email");

        request.Headers.Add(
            "api-key",
            apiKey);

        request.Headers.Add(
            "accept",
            "application/json");

        request.Content =
            JsonContent.Create(requisicao);

        using var resposta =
            await _httpClient.SendAsync(request);

        if (!resposta.IsSuccessStatusCode)
        {
            var conteudo =
                await resposta.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Não foi possível enviar o e-mail de confirmação. " +
                $"Brevo retornou {(int)resposta.StatusCode}: {conteudo}");
        }
    }
}