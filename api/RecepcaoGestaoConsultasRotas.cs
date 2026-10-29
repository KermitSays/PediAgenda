using PediAgenda.Nucleo.Repositorios;

namespace PediAgenda.Api;

/// <summary>Consultas administrativas: acesso apenas à Recepção autenticada.</summary>
public static class RecepcaoGestaoConsultasRotas
{
    public static void MapearGestaoConsultasRecepcao(this WebApplication app)
    {
        var rotas = app.MapGroup("/api/recepcao/consultas")
            .RequireAuthorization(p => p.RequireRole("RECEPCIONISTA"));

        rotas.MapGet("/{id:int}", (int id, RecepcaoGestaoConsultaRepositorioMySql repo) =>
        {
            if (id <= 0)
                return Results.BadRequest(new { codigo = "DADOS_INVALIDOS", mensagem = "Consulta inválida." });

            var c = repo.Buscar(id);
            return c is null
                ? Results.NotFound(new { codigo = "CONSULTA_NAO_ENCONTRADA", mensagem = "Consulta não encontrada." })
                : Results.Ok(new
                {
                    id = c.Id,
                    paciente = c.Paciente,
                    responsavel = c.Responsavel,
                    medico = c.Medico,
                    data = c.Data.ToString("yyyy-MM-dd"),
                    inicio = c.Inicio.ToString("HH:mm"),
                    status = c.Status
                });
        });

        rotas.MapPatch("/{id:int}/cancelar", (
            int id, RecepcaoGestaoConsultaRepositorioMySql repo, TimeProvider relogio) =>
        {
            if (id <= 0)
                return Results.BadRequest(new { codigo = "DADOS_INVALIDOS", mensagem = "Consulta inválida." });

            var resultado = repo.Cancelar(id, relogio.GetLocalNow().DateTime);
            return resultado switch
            {
                ResultadoCancelarConsultaRecepcao.Cancelada => Results.Ok(new
                {
                    id,
                    status = "CANCELADA",
                    mensagem = "Consulta cancelada. O horário foi liberado para novos agendamentos."
                }),
                ResultadoCancelarConsultaRecepcao.NaoEncontrada => Results.NotFound(new
                {
                    codigo = "CONSULTA_NAO_ENCONTRADA", mensagem = "Consulta não encontrada."
                }),
                ResultadoCancelarConsultaRecepcao.JaComecou => Results.Conflict(new
                {
                    codigo = "CONSULTA_JA_COMECOU", mensagem = "Não é possível cancelar uma consulta que já começou."
                }),
                _ => Results.Conflict(new
                {
                    codigo = "STATUS_NAO_PERMITE", mensagem = "A consulta já está encerrada ou cancelada."
                })
            };
        });
    }
}
