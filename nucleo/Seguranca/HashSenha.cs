using System.Security.Cryptography;

namespace PediAgenda.Nucleo.Seguranca;

/// <summary>
/// Geração e verificação de hash de senha com PBKDF2-SHA256.
/// Usa apenas a biblioteca padrão do .NET — nenhum pacote externo,
/// atendendo à restrição de orçamento zero do projeto.
/// </summary>
public static class HashSenha
{
    public const int TamanhoSalt = 16;   // 128 bits
    public const int TamanhoHash = 32;   // 256 bits
    public const int IteracoesPadrao = 100_000;

    public static (byte[] hash, byte[] salt, int iteracoes) Gerar(string senha, int iteracoes = IteracoesPadrao)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Derivar(senha, salt, iteracoes);
        return (hash, salt, iteracoes);
    }

    public static bool Conferir(string senha, byte[] hashEsperado, byte[] salt, int iteracoes)
    {
        var hash = Derivar(senha, salt, iteracoes);
        // Comparação em tempo constante: não vaza informação pelo tempo de resposta.
        return CryptographicOperations.FixedTimeEquals(hash, hashEsperado);
    }

    private static byte[] Derivar(string senha, byte[] salt, int iteracoes) =>
        Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
}
