using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Seguranca;

namespace PediAgenda.Nucleo.Servicos;

public enum MotivoFalha
{
    Nenhum,
    CredenciaisInvalidas,
    ContaBloqueada,
    ContaInativa,
    SenhaForaDaPolitica
}

public record ResultadoLogin(
    bool Sucesso,
    string Mensagem,
    MotivoFalha Motivo,
    Usuario? Usuario = null)
{
    public static ResultadoLogin Ok(Usuario u) =>
        new(
            true,
            $"Bem-vindo(a), {u.Nome}.",
            MotivoFalha.Nenhum,
            u);

    public static ResultadoLogin Falha(
        string mensagem,
        MotivoFalha motivo) =>
        new(
            false,
            mensagem,
            motivo);
}

/// <summary>
/// Regra de autenticação do PediAgenda.
///
/// RF03 — login com e-mail e senha.
/// RF04 — mensagem de erro para credenciais inválidas.
/// RNF02 — bloqueio após 5 tentativas inválidas.
///
/// Responsáveis pré-cadastrados pela clínica podem existir
/// sem e-mail e senha até concluírem o acesso pelo aplicativo.
/// </summary>
public class AutenticacaoService(
    IUsuarioRepositorio repositorio,
    TimeProvider? relogio = null)
{
    public const int MaximoTentativas = 5;

    public static readonly TimeSpan DuracaoBloqueio =
        TimeSpan.FromMinutes(15);

    private readonly IUsuarioRepositorio _repo =
        repositorio;

    private readonly TimeProvider _relogio =
        relogio ?? RelogioClinica.Instancia;

    // Mensagem genérica para não revelar se determinado
    // e-mail existe ou não no banco.
    private const string MensagemGenerica =
        "E-mail ou senha inválidos.";

    public ResultadoLogin Autenticar(
        string? email,
        string? senha)
    {
        var agora =
            _relogio.GetUtcNow().UtcDateTime;

        email =
            (email ?? "").Trim();

        if (email.Length is 0 or > 160)
            return ResultadoLogin.Falha(MensagemGenerica, MotivoFalha.CredenciaisInvalidas);

        // A mesma política de senha usada no cadastro
        // também é utilizada no login.
        if (!PoliticaSenha.Valida(
            senha,
            out var erroSenha))
        {
            _repo.RegistrarTentativa(
                email,
                null,
                false,
                "senha fora da politica");

            return ResultadoLogin.Falha(
                erroSenha,
                MotivoFalha.SenhaForaDaPolitica);
        }

        var usuario =
            _repo.BuscarPorEmail(email);

        if (usuario is null)
        {
            _repo.RegistrarTentativa(
                email,
                null,
                false,
                "email inexistente");

            return ResultadoLogin.Falha(
                MensagemGenerica,
                MotivoFalha.CredenciaisInvalidas);
        }

        // Um responsável pode estar registrado na clínica,
        // mas ainda não ter concluído o cadastro de acesso
        // ao aplicativo.
        //
        // Nesse caso não existe hash/salt de senha e,
        // portanto, a API não deve tentar conferir a senha.
        if (!usuario.PossuiCredenciais)
        {
            _repo.RegistrarTentativa(
                email,
                usuario.Id,
                false,
                "cadastro nao concluido");

            return ResultadoLogin.Falha(
                "O acesso ao aplicativo ainda não foi ativado para esta conta.",
                MotivoFalha.ContaInativa);
        }

        if (usuario.EstaBloqueado(agora))
        {
            var restante =
                (int)Math.Ceiling(
                    (usuario.BloqueadoAte!.Value - agora)
                    .TotalMinutes);

            _repo.RegistrarTentativa(
                email,
                usuario.Id,
                false,
                "conta bloqueada");

            return ResultadoLogin.Falha(
                $"Conta temporariamente bloqueada. " +
                $"Tente novamente em {restante} minuto(s).",
                MotivoFalha.ContaBloqueada);
        }

        // Conta inativa por qualquer outro motivo.
        if (!usuario.Ativo)
        {
            _repo.RegistrarTentativa(
                email,
                usuario.Id,
                false,
                "conta inativa");

            return ResultadoLogin.Falha(
                "Conta inativa. Procure a recepção da clínica.",
                MotivoFalha.ContaInativa);
        }

        // Neste ponto PossuiCredenciais já garantiu
        // que hash e salt existem.
        if (!HashSenha.Conferir(
            senha!,
            usuario.SenhaHash!,
            usuario.SenhaSalt!,
            usuario.SenhaIteracoes))
        {
            var (bloqueado, restantes) = _repo.RegistrarFalhaSenha(
                usuario.Id, agora, MaximoTentativas, DuracaoBloqueio);
            _repo.RegistrarTentativa(email, usuario.Id, false,
                bloqueado ? "bloqueio por tentativas" : "senha incorreta");
            return bloqueado
                ? ResultadoLogin.Falha(
                    $"Conta bloqueada por {DuracaoBloqueio.TotalMinutes:0} minutos após {MaximoTentativas} tentativas inválidas.",
                    MotivoFalha.ContaBloqueada)
                : ResultadoLogin.Falha($"{MensagemGenerica} Tentativas restantes: {restantes}.",
                    MotivoFalha.CredenciaisInvalidas);
        }

        // Login realizado com sucesso.
        usuario.TentativasInvalidas = 0;
        usuario.BloqueadoAte = null;

        _repo.Atualizar(usuario);

        _repo.RegistrarTentativa(
            email,
            usuario.Id,
            true,
            null);

        return ResultadoLogin.Ok(usuario);
    }
}