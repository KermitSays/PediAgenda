using Microsoft.AspNetCore.Diagnostics;
using MySqlConnector;

namespace PediAgenda.Api;

public sealed class TratamentoErros(ILogger<TratamentoErros> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception erro,
        CancellationToken cancelamento)
    {
        var (status, codigo, mensagem) = erro switch
        {
            BadHttpRequestException => (400, "REQUISICAO_INVALIDA", "Confira o JSON, os parâmetros e o tipo dos campos enviados."),
            MySqlException e when e.ErrorCode is MySqlErrorCode.LockDeadlock or MySqlErrorCode.LockWaitTimeout =>
                (409, "OPERACAO_CONCORRENTE", "Os dados foram alterados por outra operação. Atualize e tente novamente."),
            MySqlException => (503, "BANCO_INDISPONIVEL", "Não foi possível acessar o banco. Tente novamente em instantes."),
            _ => (500, "ERRO_INTERNO", "Não foi possível concluir a operação.")
        };
        if (status >= 500) logger.LogError(erro, "Falha na operação {Metodo} {Rota}",
            contexto.Request.Method, contexto.Request.Path);
        contexto.Response.StatusCode = status;
        await contexto.Response.WriteAsJsonAsync(new { codigo, mensagem }, cancelamento);
        return true;
    }
}
