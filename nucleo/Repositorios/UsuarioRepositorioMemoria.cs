using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Seguranca;

namespace PediAgenda.Nucleo.Repositorios;

/// <summary>
/// Implementação em memória, para demonstrar e testar o login sem banco.
/// Será substituída pela versão MySQL quando as tabelas de
/// banco/01_estrutura_usuarios.sql estiverem criadas.
/// </summary>
public class UsuarioRepositorioMemoria : IUsuarioRepositorio
{
    private readonly List<Usuario> _usuarios = [];
    private readonly List<string> _auditoria = [];

    public IReadOnlyList<string> Auditoria => _auditoria;

    public UsuarioRepositorioMemoria()
    {
        Semear("Joab Antonio de Souza", "12345678901", "joab@pediagenda.local", "senhaForte123", Perfil.Recepcionista);
        Semear("Ana Paula Ribeiro", "98765432100", "ana.responsavel@pediagenda.local", "minhaSenha88", Perfil.Responsavel);
    }

    private void Semear(string nome, string cpf, string email, string senha, Perfil perfil)
    {
        var (hash, salt, iteracoes) = HashSenha.Gerar(senha);
        _usuarios.Add(new Usuario
        {
            Id = _usuarios.Count + 1,
            Nome = nome,
            Cpf = cpf,
            Email = email,
            Perfil = perfil,
            SenhaHash = hash,
            SenhaSalt = salt,
            SenhaIteracoes = iteracoes
        });
    }

    public Usuario? BuscarPorEmail(string email) =>
        _usuarios.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    public void Atualizar(Usuario usuario)
    {
        // Em memória o objeto já é o mesmo da lista; nada a persistir.
    }

    public void RegistrarTentativa(string emailInformado, int? usuarioId, bool sucesso, string? motivo) =>
        _auditoria.Add($"{DateTime.Now:HH:mm:ss}  {emailInformado,-34}  " +
                       $"{(sucesso ? "SUCESSO" : "FALHA  ")}  {motivo ?? ""}");
}
