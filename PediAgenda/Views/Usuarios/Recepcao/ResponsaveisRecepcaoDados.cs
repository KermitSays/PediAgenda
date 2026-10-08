namespace PediAgenda.Views.Usuarios.Recepcao;

public static class ResponsaveisRecepcaoDados
{
    // =============================================
    // RESPONSÁVEIS LOCAIS
    // =============================================
    //
    // Estes dados existem apenas para permitir
    // testar a tela sem depender da API.
    //
    // Futuramente a busca será feita pela API,
    // usando os dados reais de usuario/responsavel.

    public static List<ResponsavelRecepcaoItem>
        Responsaveis
    {
        get;
    } =
        new()
        {
            new ResponsavelRecepcaoItem
            {
                Id = 1,
                Nome = "Ana Oliveira",
                Cpf = "12345678901",
                Telefone = "11999991111"
            },

            new ResponsavelRecepcaoItem
            {
                Id = 2,
                Nome = "Carla Souza",
                Cpf = "23456789012",
                Telefone = "11988882222"
            },

            new ResponsavelRecepcaoItem
            {
                Id = 3,
                Nome = "Mariana Santos",
                Cpf = "34567890123",
                Telefone = "11977773333"
            },

            new ResponsavelRecepcaoItem
            {
                Id = 4,
                Nome = "Fernanda Lima",
                Cpf = "45678901234",
                Telefone = "11966664444"
            }
        };


    // =============================================
    // BUSCA POR CPF
    // =============================================

    public static ResponsavelRecepcaoItem?
        BuscarPorCpf(
            string cpfInformado)
    {
        string cpf =
            SomenteNumeros(
                cpfInformado);


        if (cpf.Length != 11)
            return null;


        return Responsaveis
            .FirstOrDefault(r =>
                SomenteNumeros(
                    r.Cpf)
                ==
                cpf);
    }


    private static string SomenteNumeros(
        string texto)
    {
        return
            new string(
                texto
                    .Where(char.IsDigit)
                    .ToArray());
    }
}