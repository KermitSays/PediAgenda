namespace PediAgenda.Dados;

public static class SessaoUsuario
{
    public static int IdUsuario
    {
        get;
        private set;
    }


    public static string Nome
    {
        get;
        private set;
    } = string.Empty;


    public static string Email
    {
        get;
        private set;
    } = string.Empty;


    public static string Perfil
    {
        get;
        private set;
    } = string.Empty;


    public static bool EstaLogado =>
        !string.IsNullOrWhiteSpace(Nome) &&
        !string.IsNullOrWhiteSpace(Perfil);


    public static string PrimeiroNome
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Nome))
            {
                return string.Empty;
            }


            return Nome
                .Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?? Nome;
        }
    }


    public static void Iniciar(
        int idUsuario,
        string nome,
        string email,
        string perfil)
    {
        IdUsuario =
            idUsuario;


        Nome =
            nome?.Trim()
            ?? string.Empty;


        Email =
            email?.Trim()
            ?? string.Empty;


        Perfil =
            perfil?.Trim()
            ?? string.Empty;
    }


    public static void Limpar()
    {
        IdUsuario =
            0;


        Nome =
            string.Empty;


        Email =
            string.Empty;


        Perfil =
            string.Empty;
    }
}