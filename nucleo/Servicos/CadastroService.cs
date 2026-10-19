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
    IReadOnlyDictionary<string, string>? Campos = null)
{
    public static ResultadoCadastro Ok(Usuario usuario) =>
        new(true, $"Cadastro concluído para {usuario.Nome}.", MotivoRecusa.Nenhum, usuario);

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

        // Responsáveis com cadastro completo e ativo não podem se cadastrar de novo.
        // Cadastros antigos que aguardavam e-mail podem ser refeitos com o código
        // da clínica, desde que ele ainda não tenha sido utilizado.
        if (responsavel.Ativo && responsavel.PossuiCredenciais)
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.CadastroJaIniciado,
                "O acesso deste responsável já está ativo.");
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

        try
        {
            // Completa o usuário já existente e consome o código de ativação
            // na mesma instrução SQL (atômica), sem gerar token de e-mail.
            _repo.CompletarCadastroResponsavel(
                responsavel.Id, email, dados.Senha!, agora);
        }
        catch (CadastroDuplicadoException)
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.EmailJaCadastrado,
                "Já existe uma conta utilizando esse e-mail.");
        }
        catch (CodigoAtivacaoIndisponivelException)
        {
            return ResultadoCadastro.Falha(
                MotivoRecusa.CodigoAtivacaoInvalido,
                "Código de ativação inválido, expirado ou já utilizado.");
        }

        responsavel.Email = email;
        responsavel.Ativo = true;
        return ResultadoCadastro.Ok(responsavel);
    }

    private static string SoDigitos(
        string valor) =>
        new(
            [.. valor.Where(char.IsDigit)]);

    [GeneratedRegex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();
}