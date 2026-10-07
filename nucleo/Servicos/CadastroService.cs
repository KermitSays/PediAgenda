using System.Text.RegularExpressions;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Seguranca;

namespace PediAgenda.Nucleo.Servicos;

public enum MotivoRecusa
{
    Nenhum,
    DadosInvalidos,
    EmailJaCadastrado,
    ResponsavelNaoEncontrado,
    CodigoAtivacaoInvalido,
    CadastroJaIniciado
}

public record DadosResponsavel(
    string? Cpf,
    string? CodigoAtivacao,
    string? Email,
    string? Senha,
    bool AceiteTermos);

public record ResultadoCadastro(
    bool Sucesso,
    string Mensagem,
    MotivoRecusa Motivo,
    Usuario? Usuario = null,
    IReadOnlyDictionary<string, string>? Campos = null,

    // Será utilizado depois pelo serviço responsável
    // por enviar a mensagem de confirmação por e-mail.
    string? TokenVerificacaoEmail = null)
{
    public static ResultadoCadastro Ok(
        Usuario usuario,
        string token) =>
        new(
            true,
            $"Cadastro iniciado para {usuario.Nome}. Confirme o e-mail para ativar a conta.",
            MotivoRecusa.Nenhum,
            usuario,
            TokenVerificacaoEmail: token);

    public static ResultadoCadastro Invalido(
        IReadOnlyDictionary<string, string> campos) =>
        new(
            false,
            "Verifique os campos destacados.",
            MotivoRecusa.DadosInvalidos,
            Campos: campos);

    public static ResultadoCadastro Falha(
        MotivoRecusa motivo,
        string mensagem) =>
        new(
            false,
            mensagem,
            motivo);
}


/// Conclusão do cadastro de acesso do responsável.
///
/// O responsável já deve ter sido previamente cadastrado
/// pela clínica. O aplicativo não cria uma nova pessoa:
/// ele localiza o cadastro existente pelo CPF e completa
/// as credenciais de acesso.
public partial class CadastroService(
    IUsuarioRepositorio repositorio)
{
    public const int TamanhoMaximoEmail = 160;

    public static readonly TimeSpan DuracaoTokenEmail =
        TimeSpan.FromHours(24);

    private readonly IUsuarioRepositorio _repo =
        repositorio;

    public ResultadoCadastro Cadastrar(
        DadosResponsavel dados)
    {
        var cpf =
            SoDigitos(dados.Cpf ?? "");

        var codigoAtivacao =
            (dados.CodigoAtivacao ?? "").Trim();

        var email =
            (dados.Email ?? "").Trim();

        var campos =
            new Dictionary<string, string>();

        // CPF
        if (cpf.Length != 11)
        {
            campos["cpf"] =
                "O CPF deve ter 11 dígitos.";
        }

        // Código entregue pela clínica
        if (string.IsNullOrWhiteSpace(codigoAtivacao))
        {
            campos["codigoAtivacao"] =
                "Informe o código de ativação fornecido pela clínica.";
        }

        // E-mail
        if (string.IsNullOrWhiteSpace(email))
        {
            campos["email"] =
                "O e-mail é obrigatório.";
        }
        else if (email.Length > TamanhoMaximoEmail)
        {
            campos["email"] =
                $"O e-mail deve ter no máximo {TamanhoMaximoEmail} caracteres.";
        }
        else if (!EmailValido().IsMatch(email))
        {
            campos["email"] =
                "E-mail inválido.";
        }

        // Senha
        if (!PoliticaSenha.Valida(
            dados.Senha,
            out var erroSenha))
        {
            campos["senha"] =
                erroSenha;
        }

        // Termos
        if (!dados.AceiteTermos)
        {
            campos["aceiteTermos"] =
                "É preciso aceitar os termos de uso.";
        }

        if (campos.Count > 0)
        {
            return ResultadoCadastro.Invalido(
                campos);
        }

        // O CPF precisa pertencer a um RESPONSÁVEL que
        // já tenha sido cadastrado pela clínica.
        var responsavel =
            _repo.BuscarResponsavelPorCpf(cpf);

        if (responsavel is null)
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.ResponsavelNaoEncontrado,
                "Não foi encontrado um responsável cadastrado pela clínica com esse CPF.");
        }

        // Se a conta já foi efetivamente ativada, não permite
        // iniciar o cadastro novamente.
        //
        // Se ela apenas possui e-mail/senha, mas ainda não confirmou
        // o e-mail, permitimos uma nova tentativa. Isso é importante
        // caso o envio do e-mail tenha falhado ou não tenha chegado.
        if (responsavel.EmailVerificado || responsavel.Ativo)
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.CadastroJaIniciado,
                "O acesso deste responsável já está ativo.");
        }

        // Não permite utilizar um e-mail que já pertence
        // a outro usuário do sistema.
        if (_repo.ExisteEmailDeOutroUsuario(
            email,
            responsavel.Id))
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.EmailJaCadastrado,
                "Já existe outra conta utilizando esse e-mail.");
        }

        var agora =
            DateTime.UtcNow;

        // CPF sozinho não é suficiente.
        // Também precisa apresentar o código entregue
        // pela clínica no momento do pré-cadastro.
        if (!_repo.CodigoAtivacaoResponsavelValido(
            responsavel.Id,
            codigoAtivacao,
            agora))
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.CodigoAtivacaoInvalido,
                "Código de ativação inválido, expirado ou já utilizado.");
        }

        // Token aleatório que depois será enviado para
        // o endereço de e-mail informado pelo responsável.
        var (tokenEmail, tokenEmailHash) =
            TokenVerificacaoEmail.Gerar();

        var tokenExpiraEm =
            agora.Add(DuracaoTokenEmail);

        try
        {
            // Preenche e-mail + senha no MESMO usuário
            // anteriormente cadastrado pela clínica.
            _repo.CompletarCadastroResponsavel(
                responsavel.Id,
                email,
                dados.Senha!,
                tokenEmailHash,
                tokenExpiraEm);
        }
        catch (CadastroDuplicadoException)
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.EmailJaCadastrado,
                "Já existe uma conta utilizando esse e-mail.");
        }

        responsavel.Email =
            email;

        // Continua inativo até a confirmação do e-mail.
        responsavel.Ativo =
            false;

        responsavel.EmailVerificado =
            false;

        responsavel.TokenVerificacaoHash =
            tokenEmailHash;

        responsavel.TokenVerificacaoExpiraEm =
            tokenExpiraEm;

        return ResultadoCadastro.Ok(
            responsavel,
            tokenEmail);
    }

    private static string SoDigitos(
        string valor) =>
        new(
            [.. valor.Where(char.IsDigit)]);

    [GeneratedRegex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();
}