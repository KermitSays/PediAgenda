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
            EmailVerificado = true
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

    public void Atualizar(Usuario usuario)
    {
        // Como trabalhamos com o mesmo objeto armazenado na lista,
        // as alterações já ficam refletidas em memória.
    }

    public (bool Bloqueado, int Restantes) RegistrarFalhaSenha(
        int usuarioId, DateTime agora, int limite, TimeSpan duracao)
    {
        lock (_usuarios)
        {
            var usuario = _usuarios.Single(u => u.Id == usuarioId);
            if (usuario.EstaBloqueado(agora)) return (true, 0);
            usuario.TentativasInvalidas++;
            if (usuario.TentativasInvalidas >= limite)
            {
                usuario.TentativasInvalidas = 0;
                usuario.BloqueadoAte = agora.Add(duracao);
                return (true, 0);
            }
            return (false, limite - usuario.TentativasInvalidas);
        }
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
    public bool CompletarCadastroResponsavel(int usuarioId, string email, string senha, byte[] tokenHash, DateTime tokenExpiraEm)
    {
        lock (_usuarios)
        {
            if (!PoliticaSenha.Valida(senha, out var erro))
                throw new ArgumentException(erro, nameof(senha));
            var usuario = _usuarios.FirstOrDefault(u => u.Id == usuarioId
                && u.Perfil == Perfil.Responsavel
                && string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)
                && !u.Ativo && !u.EmailVerificado);
            if (usuario is null)
                return false;
            var (hash, salt, iteracoes) = HashSenha.Gerar(senha);
            usuario.SenhaHash = hash;
            usuario.SenhaSalt = salt;
            usuario.SenhaIteracoes = iteracoes;
            usuario.Ativo = false;
            usuario.EmailVerificado = false;
            usuario.EmailVerificadoEm = null;
            usuario.TokenVerificacaoHash = tokenHash;
            usuario.TokenVerificacaoExpiraEm = tokenExpiraEm;
            usuario.TentativasInvalidas = 0;
            usuario.BloqueadoAte = null;
            return true;
        }
    }

    public bool ConfirmarEmailResponsavel(byte[] tokenHash, DateTime confirmadoEm)
    {
        lock (_usuarios)
        {
            var u = _usuarios.FirstOrDefault(u => u.Perfil == Perfil.Responsavel
                && !u.EmailVerificado && !u.Ativo
                && u.TokenVerificacaoHash is { Length: 32 }
                && u.TokenVerificacaoExpiraEm >= confirmadoEm
                && CryptographicOperations.FixedTimeEquals(u.TokenVerificacaoHash, tokenHash));
            if (u is null) return false;
            u.EmailVerificado = true;
            u.EmailVerificadoEm = confirmadoEm;
            u.Ativo = true;
            u.TokenVerificacaoHash = null;
            u.TokenVerificacaoExpiraEm = null;
            u.TentativasInvalidas = 0;
            u.BloqueadoAte = null;
            return true;
        }
    }

    public int PreCadastrarResponsavel(string nome, string cpf, string email, string telefone)
    {
        lock (_usuarios)
        {
            if (ExisteEmail(email)) throw new CadastroDuplicadoException("e-mail");
            if (ExisteCpf(cpf)) throw new CadastroDuplicadoException("CPF");
            var id = _usuarios.Count + 1;
            _usuarios.Add(new Usuario
            {
                Id = id, Nome = nome, Cpf = cpf, Email = email,
                Perfil = Perfil.Responsavel, Ativo = false
            });
            return id;
        }
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