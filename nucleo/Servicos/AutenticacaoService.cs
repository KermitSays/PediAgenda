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

public record ResultadoLogin(bool Sucesso, string Mensagem, MotivoFalha Motivo, Usuario? Usuario = null)
{
    public static ResultadoLogin Ok(Usuario u) =>
        new(true, $"Bem-vindo(a), {u.Nome}.", MotivoFalha.Nenhum, u);

    public static ResultadoLogin Falha(string mensagem, MotivoFalha motivo) =>
        new(false, mensagem, motivo);
}

/// <summary>
/// Regra de autenticação do PediAgenda.
/// RF03 — login com e-mail e senha.
/// RF04 — mensagem de erro para credenciais inválidas.
/// RNF02 — bloqueio após 5 tentativas inválidas.
/// </summary>
public class AutenticacaoService(IUsuarioRepositorio repositorio, TimeProvider? relogio = null)
{
    public const int MaximoTentativas = 5;
    public static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(15);

    private readonly IUsuarioRepositorio _repo = repositorio;
    private readonly TimeProvider _relogio = relogio ?? TimeProvider.System;

    // Mensagem propositalmente genérica: não revela se o e-mail existe na
    // base, o que evitaria que alguém descobrisse cadastros por tentativa.
    private const string MensagemGenerica = "E-mail ou senha inválidos.";

    public ResultadoLogin Autenticar(string? email, string? senha)
    {
        var agora = _relogio.GetLocalNow().DateTime;
        email = (email ?? "").Trim();

        if (!PoliticaSenha.Valida(senha, out var erroSenha))
        {
            _repo.RegistrarTentativa(email, null, false, "senha fora da politica");
            return ResultadoLogin.Falha(erroSenha, MotivoFalha.SenhaForaDaPolitica);
        }

        var usuario = _repo.BuscarPorEmail(email);
        if (usuario is null)
        {
            _repo.RegistrarTentativa(email, null, false, "email inexistente");
            return ResultadoLogin.Falha(MensagemGenerica, MotivoFalha.CredenciaisInvalidas);
        }

        if (usuario.EstaBloqueado(agora))
        {
            var restante = (int)Math.Ceiling((usuario.BloqueadoAte!.Value - agora).TotalMinutes);
            _repo.RegistrarTentativa(email, usuario.Id, false, "conta bloqueada");
            return ResultadoLogin.Falha(
                $"Conta temporariamente bloqueada. Tente novamente em {restante} minuto(s).",
                MotivoFalha.ContaBloqueada);
        }

        if (!usuario.Ativo)
        {
            _repo.RegistrarTentativa(email, usuario.Id, false, "conta inativa");
            return ResultadoLogin.Falha("Conta inativa. Procure a recepção da clínica.",
                                        MotivoFalha.ContaInativa);
        }

        if (!HashSenha.Conferir(senha!, usuario.SenhaHash, usuario.SenhaSalt, usuario.SenhaIteracoes))
        {
            usuario.TentativasInvalidas++;
            var motivo = "senha incorreta";

            if (usuario.TentativasInvalidas >= MaximoTentativas)
            {
                usuario.BloqueadoAte = agora.Add(DuracaoBloqueio);
                usuario.TentativasInvalidas = 0;
                motivo = "bloqueio por tentativas";
                _repo.Atualizar(usuario);
                _repo.RegistrarTentativa(email, usuario.Id, false, motivo);
                return ResultadoLogin.Falha(
                    $"Conta bloqueada por {DuracaoBloqueio.TotalMinutes:0} minutos após " +
                    $"{MaximoTentativas} tentativas inválidas.",
                    MotivoFalha.ContaBloqueada);
            }

            _repo.Atualizar(usuario);
            _repo.RegistrarTentativa(email, usuario.Id, false, motivo);
            var restantes = MaximoTentativas - usuario.TentativasInvalidas;
            return ResultadoLogin.Falha($"{MensagemGenerica} Tentativas restantes: {restantes}.",
                                        MotivoFalha.CredenciaisInvalidas);
        }

        usuario.TentativasInvalidas = 0;
        usuario.BloqueadoAte = null;
        // O último acesso não é gravado em usuario: fica registrado em tentativa_login.
        _repo.Atualizar(usuario);
        _repo.RegistrarTentativa(email, usuario.Id, true, null);
        return ResultadoLogin.Ok(usuario);
    }
}
