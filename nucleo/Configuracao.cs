namespace PediAgenda.Nucleo;

/// <summary>
/// A string de conexão vem de uma variável de ambiente, e NÃO do código.
/// Assim a senha do banco não é gravada em arquivo nem vai parar no
/// repositório do grupo por descuido.
///
/// Definir uma vez (PowerShell), trocando SUA_SENHA:
///   setx PEDIAGENDA_CONEXAO "Server=localhost;Port=3306;Database=pediagenda;User ID=root;Password=SUA_SENHA;"
/// Depois feche e reabra o terminal.
/// </summary>
public static class Configuracao
{
    public const string VariavelAmbiente = "PEDIAGENDA_CONEXAO";

    public static string? StringDeConexao =>
        Environment.GetEnvironmentVariable(VariavelAmbiente);

    public static bool TemBanco => !string.IsNullOrWhiteSpace(StringDeConexao);
}
