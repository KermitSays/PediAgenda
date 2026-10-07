using System.Security.Cryptography;
using System.Text;

namespace PediAgenda.Nucleo.Seguranca;

/// <summary>
/// Geração e verificação do código entregue pela clínica
/// ao responsável para permitir a ativação do acesso ao app.
/// </summary>
public static class CodigoAtivacao
{
    public static byte[] GerarHash(string codigo)
    {
        var valor =
            (codigo ?? "").Trim();

        return SHA256.HashData(
            Encoding.UTF8.GetBytes(valor));
    }

    public static bool Conferir(
        string codigo,
        byte[] hashEsperado)
    {
        var hashInformado =
            GerarHash(codigo);

        return CryptographicOperations.FixedTimeEquals(
            hashInformado,
            hashEsperado);
    }
}