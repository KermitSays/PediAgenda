using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PediAgenda.Api.Seguranca;

/// <summary>
/// Parâmetros do token (JWT) que a API entrega no login.
///
/// A chave que assina o token vem da variável de ambiente PEDIAGENDA_JWT_CHAVE,
/// nunca do código: quem tiver a chave consegue fabricar um token de qualquer
/// usuário, inclusive de médico.
/// </summary>
public static class ConfiguracaoToken
{
    public const string VariavelAmbiente = "PEDIAGENDA_JWT_CHAVE";
    public const string Emissor = "PediAgenda.Api";
    public const string Publico = "PediAgenda.App";

    // Depois disso o app precisa pedir login de novo.
    public static readonly TimeSpan Validade = TimeSpan.FromHours(8);

    // A assinatura é HMAC-SHA256, que exige chave de pelo menos 256 bits.
    public const int TamanhoMinimoChave = 32;

    public static string? Chave => Environment.GetEnvironmentVariable(VariavelAmbiente);

    public static bool ChaveValida =>
        Chave is { } c && Encoding.UTF8.GetByteCount(c) >= TamanhoMinimoChave;

    public static SymmetricSecurityKey ChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(Chave!));
}

/// <summary>
/// Gera o token entregue no login e no cadastro.
///
/// O token carrega quem é o usuário (id, nome, e-mail) e o perfil. É o perfil
/// que as próximas rotas vão conferir: responsável vê só as consultas dele,
/// médico vê a agenda dele, recepção vê o dia.
/// </summary>
public class GeradorToken(TimeProvider relogio)
{
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTime ExpiraEm) Gerar(int id, string nome, string email, string perfil)
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        var expiraEm = agora.Add(ConfiguracaoToken.Validade);

        var descritor = new SecurityTokenDescriptor
        {
            Issuer = ConfiguracaoToken.Emissor,
            Audience = ConfiguracaoToken.Publico,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = expiraEm,
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", id.ToString()),
                new Claim("name", nome),
                new Claim("email", email),
                new Claim("role", perfil)
            ]),
            SigningCredentials = new SigningCredentials(
                ConfiguracaoToken.ChaveDeAssinatura(), SecurityAlgorithms.HmacSha256)
        };

        return (_handler.CreateToken(descritor), expiraEm);
    }
}
