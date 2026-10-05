using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Seguranca;

namespace PediAgenda.Nucleo.Servicos;

public enum MotivoFalhaConfirmacaoEmail
{
    Nenhum,
    TokenAusente,
    TokenInvalidoOuExpirado
}

public record ResultadoConfirmacaoEmail(
    bool Sucesso,
    string Mensagem,
    MotivoFalhaConfirmacaoEmail Motivo)
{
    public static ResultadoConfirmacaoEmail Ok() =>
        new(
            true,
            "E-mail confirmado com sucesso. Sua conta já pode ser utilizada.",
            MotivoFalhaConfirmacaoEmail.Nenhum);

    public static ResultadoConfirmacaoEmail Falha(
        string mensagem,
        MotivoFalhaConfirmacaoEmail motivo) =>
        new(
            false,
            mensagem,
            motivo);
}

public class ConfirmacaoEmailService(
    IUsuarioRepositorio repositorio,
    TimeProvider? relogio = null)
{
    private readonly IUsuarioRepositorio _repo =
        repositorio;

    private readonly TimeProvider _relogio =
        relogio ?? TimeProvider.System;

    public ResultadoConfirmacaoEmail Confirmar(
        string? token)
    {
        token =
            (token ?? "").Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            return ResultadoConfirmacaoEmail.Falha(
                "O token de confirmação não foi informado.",
                MotivoFalhaConfirmacaoEmail.TokenAusente);
        }

        var tokenHash =
            TokenVerificacaoEmail.GerarHash(token);

        var agora =
            _relogio.GetUtcNow().UtcDateTime;

        var confirmado =
            _repo.ConfirmarEmailResponsavel(
                tokenHash,
                agora);

        if (!confirmado)
        {
            return ResultadoConfirmacaoEmail.Falha(
                "O link de confirmação é inválido, já foi utilizado ou expirou.",
                MotivoFalhaConfirmacaoEmail.TokenInvalidoOuExpirado);
        }

        return ResultadoConfirmacaoEmail.Ok();
    }
}