using System.Security.Cryptography;
using System.Text;

namespace PediAgenda.Nucleo.Seguranca;

/// Gera tokens aleatórios utilizados na confirmação do e-mail.
/// No banco é armazenado apenas o hash do token.
public static class TokenVerificacaoEmail
{
    public static (string token, byte[] hash) Gerar()
    {
        var bytes =
            RandomNumberGenerator.GetBytes(32);

        var token =
            Convert.ToHexString(bytes);

        var hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

        return (token, hash);
    }

    public static byte[] GerarHash(string token)
    {
        var valor =
            (token ?? "").Trim();

        return SHA256.HashData(
            Encoding.UTF8.GetBytes(valor));
    }
}