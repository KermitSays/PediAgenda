using System.Net.Mail;

namespace PediAgenda.Views.Login;

public partial class RecuperarSenha : ContentPage
{
    // =============================================
    // CÓDIGO LOCAL DE TESTE
    // =============================================
    //
    // Temporário enquanto a API ainda não possui
    // recuperação de senha.
    //
    // Futuramente:
    // - a API gera o código;
    // - envia por e-mail;
    // - valida o código e sua expiração;
    // - redefine senha_hash e senha_salt.

    private const string CodigoTesteLocal =
        "123456";


    private string emailRecuperacao =
        string.Empty;


    // =============================================
    // CONSTRUTOR
    // =============================================

    public RecuperarSenha()
    {
        InitializeComponent();


        // =========================================
        // EVENTOS DOS BOTÕES
        // =========================================
        //
        // Ligamos os eventos aqui em vez de usar
        // Clicked="..." no XAML.
        //
        // Isso evita os erros MAUIX2014 que
        // apareceram durante a compilação.

        EnviarCodigoButton.Clicked +=
            EnviarCodigoButton_Clicked;


        ConfirmarCodigoButton.Clicked +=
            ConfirmarCodigoButton_Clicked;


        ReenviarCodigoButton.Clicked +=
            ReenviarCodigoButton_Clicked;


        AlterarSenhaButton.Clicked +=
            AlterarSenhaButton_Clicked;


        VoltarButton.Clicked +=
            VoltarButton_Clicked;
    }


    // =============================================
    // ENVIAR CÓDIGO
    // =============================================

    private async void EnviarCodigoButton_Clicked(
        object? sender,
        EventArgs e)
    {
        string email =
            EmailEntry.Text?
                .Trim()
            ?? string.Empty;


        if (
            string.IsNullOrWhiteSpace(
                email))
        {
            await DisplayAlertAsync(
                "E-mail necessário",
                "Informe o e-mail cadastrado na sua conta.",
                "OK");


            return;
        }


        if (
            !EmailValido(
                email))
        {
            await DisplayAlertAsync(
                "E-mail inválido",
                "Informe um endereço de e-mail válido.",
                "OK");


            return;
        }


        emailRecuperacao =
            email;


        // =========================================
        // SIMULAÇÃO LOCAL
        // =========================================
        //
        // Nesta versão ainda não existe envio real.
        //
        // A futura integração com a API deverá
        // substituir esta parte.

        EmailMascaradoLabel.Text =
            MascararEmail(
                emailRecuperacao);


        EtapaEmailLayout.IsVisible =
            false;


        EtapaCodigoLayout.IsVisible =
            true;


        EtapaNovaSenhaLayout.IsVisible =
            false;


        DescricaoEtapaLabel.Text =
            "Digite o código de verificação enviado para seu e-mail.";


        CodigoEntry.Text =
            string.Empty;


        await DisplayAlertAsync(
            "Código enviado",
            "O código de verificação foi enviado para o e-mail informado.",
            "OK");
    }


    // =============================================
    // CONFIRMAR CÓDIGO
    // =============================================

    private async void ConfirmarCodigoButton_Clicked(
        object? sender,
        EventArgs e)
    {
        string codigo =
            CodigoEntry.Text?
                .Trim()
            ?? string.Empty;


        if (
            string.IsNullOrWhiteSpace(
                codigo))
        {
            await DisplayAlertAsync(
                "Código necessário",
                "Informe o código de verificação.",
                "OK");


            return;
        }


        if (
            codigo !=
            CodigoTesteLocal)
        {
            await DisplayAlertAsync(
                "Código inválido",
                "O código informado não é válido.",
                "OK");


            return;
        }


        EtapaEmailLayout.IsVisible =
            false;


        EtapaCodigoLayout.IsVisible =
            false;


        EtapaNovaSenhaLayout.IsVisible =
            true;


        DescricaoEtapaLabel.Text =
            "Crie uma nova senha para sua conta.";


        NovaSenhaEntry.Text =
            string.Empty;


        ConfirmarSenhaEntry.Text =
            string.Empty;
    }


    // =============================================
    // REENVIAR CÓDIGO
    // =============================================

    private async void ReenviarCodigoButton_Clicked(
        object? sender,
        EventArgs e)
    {
        if (
            string.IsNullOrWhiteSpace(
                emailRecuperacao))
        {
            VoltarParaEmail();


            return;
        }


        CodigoEntry.Text =
            string.Empty;


        await DisplayAlertAsync(
            "Código reenviado",
            "Um novo código de verificação foi enviado.",
            "OK");
    }


    // =============================================
    // ALTERAR SENHA
    // =============================================

    private async void AlterarSenhaButton_Clicked(
        object? sender,
        EventArgs e)
    {
        string novaSenha =
            NovaSenhaEntry.Text
            ?? string.Empty;


        string confirmarSenha =
            ConfirmarSenhaEntry.Text
            ?? string.Empty;


        if (
            string.IsNullOrWhiteSpace(
                novaSenha)

            ||

            string.IsNullOrWhiteSpace(
                confirmarSenha))
        {
            await DisplayAlertAsync(
                "Senha necessária",
                "Preencha a nova senha e a confirmação.",
                "OK");


            return;
        }


        if (
            novaSenha.Length <
            6)
        {
            await DisplayAlertAsync(
                "Senha muito curta",
                "A nova senha deve possuir pelo menos 6 caracteres.",
                "OK");


            return;
        }


        if (
            novaSenha !=
            confirmarSenha)
        {
            await DisplayAlertAsync(
                "Senhas diferentes",
                "A confirmação não corresponde à nova senha.",
                "OK");


            return;
        }


        // =========================================
        // FUTURA INTEGRAÇÃO COM API
        // =========================================
        //
        // Esta versão apenas simula o fluxo.
        //
        // A senha do usuário NÃO está sendo
        // alterada no MySQL neste momento.
        //
        // A futura API deverá receber:
        // - usuário/e-mail;
        // - código ou token já validado;
        // - nova senha;
        //
        // e gerar os novos senha_hash/senha_salt.

        await DisplayAlertAsync(
            "Senha alterada",
            "Senha alterada com sucesso. Você já pode entrar com sua nova senha.",
            "OK");


        await Shell.Current.GoToAsync(
            "..");
    }


    // =============================================
    // VOLTAR
    // =============================================

    private async void VoltarButton_Clicked(
        object? sender,
        EventArgs e)
    {
        // =========================================
        // NOVA SENHA → CÓDIGO
        // =========================================

        if (
            EtapaNovaSenhaLayout.IsVisible)
        {
            EtapaNovaSenhaLayout.IsVisible =
                false;


            EtapaCodigoLayout.IsVisible =
                true;


            DescricaoEtapaLabel.Text =
                "Digite o código de verificação enviado para seu e-mail.";


            return;
        }


        // =========================================
        // CÓDIGO → E-MAIL
        // =========================================

        if (
            EtapaCodigoLayout.IsVisible)
        {
            VoltarParaEmail();


            return;
        }


        // =========================================
        // E-MAIL → LOGIN
        // =========================================

        await Shell.Current.GoToAsync(
            "..");
    }


    // =============================================
    // VOLTAR PARA O E-MAIL
    // =============================================

    private void VoltarParaEmail()
    {
        EtapaEmailLayout.IsVisible =
            true;


        EtapaCodigoLayout.IsVisible =
            false;


        EtapaNovaSenhaLayout.IsVisible =
            false;


        DescricaoEtapaLabel.Text =
            "Informe o e-mail cadastrado na sua conta.";


        CodigoEntry.Text =
            string.Empty;


        NovaSenhaEntry.Text =
            string.Empty;


        ConfirmarSenhaEntry.Text =
            string.Empty;
    }


    // =============================================
    // VALIDAÇÃO DO E-MAIL
    // =============================================

    private static bool EmailValido(
        string email)
    {
        try
        {
            MailAddress endereco =
                new(
                    email);


            return endereco.Address.Equals(
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }


    // =============================================
    // MASCARA O E-MAIL
    // =============================================

    private static string MascararEmail(
        string email)
    {
        int arroba =
            email.IndexOf(
                '@');


        if (
            arroba <=
            0)
        {
            return email;
        }


        string usuario =
            email.Substring(
                0,
                arroba);


        string dominio =
            email.Substring(
                arroba);


        if (
            usuario.Length ==
            1)
        {
            return
                $"{usuario[0]}***{dominio}";
        }


        return
            $"{usuario[0]}***{usuario[^1]}{dominio}";
    }
}