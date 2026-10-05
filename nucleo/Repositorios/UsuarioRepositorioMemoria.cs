using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Seguranca;
using System.Security.Cryptography;

namespace PediAgenda.Nucleo.Repositorios;


/// Implementação em memória utilizada para testes sem MySQL.
/// Mantém as mesmas operações disponíveis no repositório real.
public class UsuarioRepositorioMemoria : IUsuarioRepositorio
{
    private readonly List<Usuario> _usuarios = [];
    private readonly List<string> _auditoria = [];

    public IReadOnlyList<string> Auditoria => _auditoria;

    public UsuarioRepositorioMemoria()
    {
        Semear(
            "Joab Antonio de Souza",
            "12345678901",
            "joab@pediagenda.local",
            "senhaForte123",
            Perfil.Recepcionista);

        Semear(
            "Ana Paula Ribeiro",
            "98765432100",
            "ana.responsavel@pediagenda.local",
            "minhaSenha88",
            Perfil.Responsavel);
    }

    private void Semear(
        string nome,
        string cpf,
        string email,
        string senha,
        Perfil perfil)
    {
        var (hash, salt, iteracoes) =
            HashSenha.Gerar(senha);

        _usuarios.Add(new Usuario
        {
            Id = _usuarios.Count + 1,
            Nome = nome,
            Cpf = cpf,
            Email = email,
            Perfil = perfil,

            SenhaHash = hash,
            SenhaSalt = salt,
            SenhaIteracoes = iteracoes,

            // Os usuários semeados já representam contas completas.
            Ativo = true,
            EmailVerificado = true,
            EmailVerificadoEm = DateTime.Now
        });
    }

    public Usuario? BuscarPorEmail(string email) =>
        _usuarios.FirstOrDefault(
            u => string.Equals(
                u.Email,
                email,
                StringComparison.OrdinalIgnoreCase));

    
    /// Procura especificamente um responsável pelo CPF.
    public Usuario? BuscarResponsavelPorCpf(string cpf) =>
        _usuarios.FirstOrDefault(
            u =>
                u.Cpf == cpf
                && u.Perfil == Perfil.Responsavel);

    public bool CodigoAtivacaoResponsavelValido(
    int usuarioId,
    string codigo,
    DateTime agora)
    {
        // A versão em memória é apenas auxiliar para testes.
        // O fluxo real de ativação utiliza o MySQL.
        return false;
    }

    public void MarcarCodigoAtivacaoComoUsado(
        int usuarioId,
        DateTime usadoEm)
    {
        // Nada a persistir na versão em memória por enquanto.
    }

    public bool ConfirmarEmailResponsavel(
    byte[] tokenHash,
    DateTime confirmadoEm)
    {
        var usuario =
            _usuarios.FirstOrDefault(
                u =>
                    u.Perfil == Perfil.Responsavel
                    && !u.EmailVerificado
                    && u.TokenVerificacaoHash is not null
                    && u.TokenVerificacaoExpiraEm is not null
                    && u.TokenVerificacaoExpiraEm >= confirmadoEm
                    && CryptographicOperations.FixedTimeEquals(
                        u.TokenVerificacaoHash,
                        tokenHash));

        if (usuario is null)
            return false;

        usuario.EmailVerificado = true;
        usuario.EmailVerificadoEm = confirmadoEm;
        usuario.Ativo = true;

        usuario.TokenVerificacaoHash = null;
        usuario.TokenVerificacaoExpiraEm = null;

        usuario.TentativasInvalidas = 0;
        usuario.BloqueadoAte = null;

        return true;
    }

    public void Atualizar(Usuario usuario)
    {
        // Como trabalhamos com o mesmo objeto armazenado na lista,
        // as alterações já ficam refletidas em memória.
    }

    public void RegistrarTentativa(
        string emailInformado,
        int? usuarioId,
        bool sucesso,
        string? motivo)
    {
        _auditoria.Add(
            $"{DateTime.Now:HH:mm:ss}  " +
            $"{emailInformado,-34}  " +
            $"{(sucesso ? "SUCESSO" : "FALHA  ")}  " +
            $"{motivo ?? ""}");
    }

    public bool ExisteEmail(string email) =>
        _usuarios.Any(
            u => string.Equals(
                u.Email,
                email,
                StringComparison.OrdinalIgnoreCase));

    public bool ExisteEmailDeOutroUsuario(
    string email,
    int usuarioId) =>
    _usuarios.Any(
        u =>
            u.Id != usuarioId
            && string.Equals(
                u.Email,
                email,
                StringComparison.OrdinalIgnoreCase));

    public bool ExisteCpf(string cpf) =>
        _usuarios.Any(
            u => u.Cpf == cpf);
   
    /// Completa o cadastro de acesso de um responsável
    /// que já havia sido pré-cadastrado.
    public void CompletarCadastroResponsavel(
        int usuarioId,
        string email,
        string senha,
        byte[] tokenVerificacaoHash,
        DateTime tokenExpiraEm)
    {
        if (ExisteEmail(email))
        {
            throw new CadastroDuplicadoException(
                "e-mail");
        }

        if (!PoliticaSenha.Valida(
            senha,
            out var erro))
        {
            throw new ArgumentException(
                erro,
                nameof(senha));
        }

        var usuario =
            _usuarios.FirstOrDefault(
                u =>
                    u.Id == usuarioId
                    && u.Perfil == Perfil.Responsavel);

        if (usuario is null)
        {
            throw new InvalidOperationException(
                "O responsável informado não foi encontrado.");
        }

        var (hash, salt, iteracoes) =
            HashSenha.Gerar(senha);

        usuario.Email = email;

        usuario.SenhaHash = hash;
        usuario.SenhaSalt = salt;
        usuario.SenhaIteracoes = iteracoes;

        // Ainda não pode entrar:
        // primeiro precisa confirmar o e-mail.
        usuario.Ativo = false;
        usuario.EmailVerificado = false;

        usuario.TokenVerificacaoHash =
            tokenVerificacaoHash;

        usuario.TokenVerificacaoExpiraEm =
            tokenExpiraEm;

        usuario.EmailVerificadoEm = null;

        usuario.TentativasInvalidas = 0;
        usuario.BloqueadoAte = null;
    }

    public int Inserir(
        string nome,
        string cpf,
        string email,
        string senha,
        Perfil perfil,
        string? telefone = null,
        string? crm = null)
    {
        if (ExisteEmail(email))
        {
            throw new CadastroDuplicadoException(
                "e-mail");
        }

        if (ExisteCpf(cpf))
        {
            throw new CadastroDuplicadoException(
                "CPF");
        }

        Semear(
            nome,
            cpf,
            email,
            senha,
            perfil);

        return _usuarios[^1].Id;
    }


}