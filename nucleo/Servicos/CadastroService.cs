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
    CpfJaCadastrado
}

public record DadosResponsavel(string? Nome, string? Cpf, string? Email,
                               string? Telefone, string? Senha, bool AceiteTermos);

public record ResultadoCadastro(bool Sucesso, string Mensagem, MotivoRecusa Motivo,
                                Usuario? Usuario = null,
                                IReadOnlyDictionary<string, string>? Campos = null)
{
    public static ResultadoCadastro Ok(Usuario u) =>
        new(true, $"Cadastro criado para {u.Nome}.", MotivoRecusa.Nenhum, u);

    public static ResultadoCadastro Invalido(IReadOnlyDictionary<string, string> campos) =>
        new(false, "Verifique os campos destacados.", MotivoRecusa.DadosInvalidos, Campos: campos);

    public static ResultadoCadastro Duplicado(MotivoRecusa motivo, string mensagem) =>
        new(false, mensagem, motivo);
}

/// <summary>
/// Cadastro de responsável (H1).
///
/// Médico e recepcionista não se cadastram sozinhos: essas contas são criadas
/// pela clínica. Por isso aqui só existe o responsável.
///
/// A regra de senha é a mesma do login, vinda de PoliticaSenha — se cada módulo
/// validasse por conta própria, as duas validações divergiriam com o tempo.
/// </summary>
public partial class CadastroService(IUsuarioRepositorio repositorio)
{
    public const int TamanhoMaximoNome = 120;
    public const int TamanhoMaximoEmail = 160;
    public const int TamanhoMaximoTelefone = 20;

    private readonly IUsuarioRepositorio _repo = repositorio;

    public ResultadoCadastro Cadastrar(DadosResponsavel dados)
    {
        var nome = (dados.Nome ?? "").Trim();
        var email = (dados.Email ?? "").Trim();
        var telefone = (dados.Telefone ?? "").Trim();
        var cpf = SoDigitos(dados.Cpf ?? "");

        var campos = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(nome))
            campos["nome"] = "O nome é obrigatório.";
        else if (nome.Length > TamanhoMaximoNome)
            campos["nome"] = $"O nome deve ter no máximo {TamanhoMaximoNome} caracteres.";

        if (cpf.Length != 11)
            campos["cpf"] = "O CPF deve ter 11 dígitos.";

        if (string.IsNullOrWhiteSpace(email))
            campos["email"] = "O e-mail é obrigatório.";
        else if (email.Length > TamanhoMaximoEmail)
            campos["email"] = $"O e-mail deve ter no máximo {TamanhoMaximoEmail} caracteres.";
        else if (!EmailValido().IsMatch(email))
            campos["email"] = "E-mail inválido.";

        // A coluna responsavel.telefone é NOT NULL: é por ele que a clínica
        // confirma a consulta.
        if (string.IsNullOrWhiteSpace(telefone))
            campos["telefone"] = "O telefone é obrigatório.";
        else if (telefone.Length > TamanhoMaximoTelefone)
            campos["telefone"] = $"O telefone deve ter no máximo {TamanhoMaximoTelefone} caracteres.";

        if (!PoliticaSenha.Valida(dados.Senha, out var erroSenha))
            campos["senha"] = erroSenha;

        if (!dados.AceiteTermos)
            campos["aceiteTermos"] = "É preciso aceitar os termos de uso.";

        if (campos.Count > 0)
            return ResultadoCadastro.Invalido(campos);

        // Conferir antes permite dizer qual campo está repetido; a garantia de
        // verdade é a restrição UNIQUE do banco, tratada logo abaixo.
        if (_repo.ExisteEmail(email))
            return ResultadoCadastro.Duplicado(MotivoRecusa.EmailJaCadastrado,
                                               "Já existe uma conta com esse e-mail.");
        if (_repo.ExisteCpf(cpf))
            return ResultadoCadastro.Duplicado(MotivoRecusa.CpfJaCadastrado,
                                               "Já existe uma conta com esse CPF.");

        int id;
        try
        {
            id = _repo.Inserir(nome, cpf, email, dados.Senha!, Perfil.Responsavel, telefone: telefone);
        }
        catch (CadastroDuplicadoException e)
        {
            var motivo = e.Campo == "CPF" ? MotivoRecusa.CpfJaCadastrado : MotivoRecusa.EmailJaCadastrado;
            return ResultadoCadastro.Duplicado(motivo, $"Já existe uma conta com esse {e.Campo}.");
        }

        return ResultadoCadastro.Ok(new Usuario
        {
            Id = id,
            Nome = nome,
            Cpf = cpf,
            Email = email,
            Perfil = Perfil.Responsavel
        });
    }

    private static string SoDigitos(string valor) =>
        new([.. valor.Where(char.IsDigit)]);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailValido();
}
