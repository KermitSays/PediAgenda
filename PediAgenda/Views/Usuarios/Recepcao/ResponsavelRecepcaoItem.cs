namespace PediAgenda.Views.Usuarios.Recepcao;

public class ResponsavelRecepcaoItem
{
    public int Id
    {
        get;
        set;
    }


    public string Nome
    {
        get;
        set;
    } = string.Empty;


    public string Cpf
    {
        get;
        set;
    } = string.Empty;


    public string Telefone
    {
        get;
        set;
    } = string.Empty;


    // =============================================
    // CPF MASCARADO
    // =============================================
    //
    // Exemplo:
    // ***.456.789-**
    //
    // A Recepção consegue confirmar visualmente
    // o cadastro encontrado sem precisar exibir
    // o CPF completo.

    public string CpfMascarado
    {
        get
        {
            string cpf =
                new string(
                    Cpf
                        .Where(char.IsDigit)
                        .ToArray());


            if (cpf.Length != 11)
                return "***.***.***-**";


            return
                $"***.{cpf.Substring(3, 3)}." +
                $"{cpf.Substring(6, 3)}-**";
        }
    }


    // =============================================
    // TELEFONE MASCARADO
    // =============================================

    public string TelefoneMascarado
    {
        get
        {
            string telefone =
                new string(
                    Telefone
                        .Where(char.IsDigit)
                        .ToArray());


            if (telefone.Length < 6)
                return "Telefone não informado";


            string ddd =
                telefone.Length >= 2
                    ? telefone.Substring(0, 2)
                    : "**";


            string ultimosQuatro =
                telefone.Length >= 4
                    ? telefone.Substring(
                        telefone.Length - 4)
                    : "****";


            return
                $"({ddd}) *****-{ultimosQuatro}";
        }
    }
}