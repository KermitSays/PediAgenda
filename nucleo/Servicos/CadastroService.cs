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
    DadosCadastraisNaoConferem,
    CadastroJaIniciado
}

public record DadosResponsavel(
    string? Cpf,
    string? Email,
    string? Senha,
    bool AceiteTermos);

public record ResultadoCadastro(
    bool Sucesso,
    string Mensagem,
    MotivoRecusa Motivo,
    Usuario? Usuario = null,
    IReadOnlyDictionary<string, string>? Campos = null,
    string? TokenVerificacaoEmail = null)
{
    public static ResultadoCadastro Ok(Usuario usuario, string? token = null) =>
        new(true, $"Cadastro iniciado para {usuario.Nome}.", MotivoRecusa.Nenhum, usuario,
            TokenVerificacaoEmail: token);

    public static ResultadoCadastro Invalido(IReadOnlyDictionary<string, string> campos) =>
        new(false, "Verifique os campos destacados.", MotivoRecusa.DadosInvalidos, Campos: campos);

    public static ResultadoCadastro Falha(MotivoRecusa motivo, string mensagem) =>
        new(false, mensagem, motivo);
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
    public static readonly TimeSpan DuracaoTokenEmail = TimeSpan.FromHours(24);

    private readonly IUsuarioRepositorio _repo =
        repositorio;

    public ResultadoCadastro Cadastrar(
        DadosResponsavel dados)
    {
        var cpf =
            SoDigitos(dados.Cpf ?? "");

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

        // Conta já ativada não pode voltar ao fluxo de cadastro.
        // Pendente de confirmação pode solicitar novo link.
        if (responsavel.Ativo || responsavel.EmailVerificado)
        {
            return ResultadoCadastro.Falha(MotivoRecusa.CadastroJaIniciado,
                "Este responsável já possui acesso ativo. Procure a recepção se precisar recuperar a conta.");
        }

        // O e-mail precisa ter sido registrado pela clínica
        // para este mesmo CPF; não pode ser escolhido livremente.
        if (string.IsNullOrWhiteSpace(responsavel.Email)
            || !string.Equals(
                responsavel.Email.Trim(),
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.DadosCadastraisNaoConferem,
                "CPF e e-mail não conferem com o cadastro da clínica. Procure a recepção.");
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

        var (token, hashToken) = TokenVerificacaoEmail.Gerar();
        var expiraEm = DateTime.UtcNow.Add(DuracaoTokenEmail);
        try
        {
            // A condição também é conferida pelo repositório de forma atômica.
            if (!_repo.CompletarCadastroResponsavel(responsavel.Id, email, dados.Senha!, hashToken, expiraEm))
            {
                return ResultadoCadastro.Falha(
                    MotivoRecusa.CadastroJaIniciado,
                    "O cadastro já foi concluído ou alterado. Procure a recepção.");
            }
        }
        catch (CadastroDuplicadoException)
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.EmailJaCadastrado,
                "Já existe uma conta utilizando esse e-mail.");
        }
        responsavel.Email = email;
        responsavel.Ativo = false;
        responsavel.EmailVerificado = false;
        return ResultadoCadastro.Ok(responsavel, token);
    }

    public ResultadoCadastro PreCadastrar(string? nomeInformado, string? cpfInformado,
                                           string? emailInformado, string? telefoneInformado)
    {
        var nome = (nomeInformado ?? "").Trim();
        var cpf = SoDigitos(cpfInformado ?? "");
        var email = (emailInformado ?? "").Trim();
        var telefone = SoDigitos(telefoneInformado ?? "");
        var campos = new Dictionary<string, string>();
        if (nome.Length is < 1 or > 120) campos["nome"] = "Informe um nome de até 120 caracteres.";
        if (cpf.Length != 11) campos["cpf"] = "O CPF deve ter 11 dígitos.";
        if (email.Length > TamanhoMaximoEmail || !EmailValido().IsMatch(email))
            campos["email"] = "Informe um e-mail válido de até 160 caracteres.";
        if (telefone.Length is < 10 or > 15) campos["telefone"] = "Informe um telefone com DDD.";
        if (campos.Count > 0) return ResultadoCadastro.Invalido(campos);
        try
        {
            var id = _repo.PreCadastrarResponsavel(nome, cpf, email, telefone);
            return ResultadoCadastro.Ok(new Usuario
            {
                Id = id, Nome = nome, Cpf = cpf, Email = email,
                Perfil = Perfil.Responsavel, Ativo = false
            });
        }
        catch (CadastroDuplicadoException e)
        {
            return ResultadoCadastro.Falha(MotivoRecusa.CadastroJaIniciado,
                $"Já existe cadastro com esse {e.Campo}.");
        }
    }

    private static string SoDigitos(
        string valor) =>
        new(
            [.. valor.Where(char.IsDigit)]);

    [GeneratedRegex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();
}