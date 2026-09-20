using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

/// <summary>
/// Contrato de acesso a dados do usuário.
///
/// O serviço de autenticação depende desta interface, e não do MySQL
/// diretamente. Assim o login já funciona e é testável antes de o banco
/// estar pronto, e a troca por uma implementação real não muda a regra.
/// </summary>
public interface IUsuarioRepositorio
{
    Usuario? BuscarPorEmail(string email);
    void Atualizar(Usuario usuario);
    void RegistrarTentativa(string emailInformado, int? usuarioId, bool sucesso, string? motivo);
}
