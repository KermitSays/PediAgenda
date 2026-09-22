namespace PediAgenda.Nucleo.Seguranca;

/// <summary>
/// RNF01 — senha com mínimo de 8 caracteres.
///
/// A regra fica AQUI, em um único lugar, e é usada tanto pelo cadastro
/// (H1, Sara/Talita) quanto pelo login. Se cada módulo validar por conta
/// própria, as duas validações divergem com o tempo.
/// </summary>
public static class PoliticaSenha
{
    public const int TamanhoMinimo = 8;

    public static bool Valida(string? senha, out string erro)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            erro = "A senha é obrigatória.";
            return false;
        }

        if (senha.Length < TamanhoMinimo)
        {
            erro = $"A senha deve ter no mínimo {TamanhoMinimo} caracteres.";
            return false;
        }

        erro = "";
        return true;
    }
}
