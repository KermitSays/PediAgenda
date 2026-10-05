using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;


/// Contrato de acesso aos dados dos usuários do PediAgenda.
public interface IUsuarioRepositorio
{
    // Usado no login.
    Usuario? BuscarPorEmail(string email);

    // Usado quando um responsável, já cadastrado pela clínica,
    // decide criar seu acesso ao aplicativo.
    Usuario? BuscarResponsavelPorCpf(string cpf);

    bool CodigoAtivacaoResponsavelValido(
    int usuarioId,
    string codigo,
    DateTime agora);

    void MarcarCodigoAtivacaoComoUsado(
        int usuarioId,
        DateTime usadoEm);

    bool ConfirmarEmailResponsavel(
    byte[] tokenHash,
    DateTime confirmadoEm);

    void Atualizar(Usuario usuario);

    void RegistrarTentativa(
        string emailInformado,
        int? usuarioId,
        bool sucesso,
        string? motivo);

    bool ExisteEmail(string email);

    bool ExisteEmailDeOutroUsuario(
    string email,
    int usuarioId);

    bool ExisteCpf(string cpf);

    
    /// Completa o acesso de um responsável que já existe no banco.
    ///
    /// Não cria outro usuário: apenas adiciona e-mail e senha ao
    /// registro que foi previamente criado pela clínica.
    void CompletarCadastroResponsavel(
        int usuarioId,
        string email,
        string senha,
        byte[] tokenVerificacaoHash,
        DateTime tokenExpiraEm);

    
    /// Cria uma conta completa.
    ///
    /// Continua sendo usado principalmente para contas criadas
    /// pela própria clínica, como médico e recepcionista.
    int Inserir(
        string nome,
        string cpf,
        string email,
        string senha,
        Perfil perfil,
        string? telefone = null,
        string? crm = null);
}


/// Exceção utilizada quando CPF ou e-mail já existem no banco.
public class CadastroDuplicadoException(string campo)
    : Exception($"Já existe cadastro com esse {campo}.")
{
    public string Campo { get; } = campo;
}