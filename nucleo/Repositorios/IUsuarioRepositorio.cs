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

    bool ExisteEmail(string email);
    bool ExisteCpf(string cpf);

    /// <summary>
    /// Cria o usuário e a especialização do perfil. Lança
    /// <see cref="CadastroDuplicadoException"/> se o e-mail ou o CPF já existirem.
    /// </summary>
    int Inserir(string nome, string cpf, string email, string senha, Perfil perfil,
                string? telefone = null, string? crm = null);
}

/// <summary>
/// E-mail ou CPF que já existe na base. Fica aqui, e não no MySqlException, para
/// que a regra de cadastro não precise conhecer o banco que está por baixo.
/// </summary>
public class CadastroDuplicadoException(string campo)
    : Exception($"Já existe cadastro com esse {campo}.")
{
    public string Campo { get; } = campo;
}
