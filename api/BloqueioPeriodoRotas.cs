using System.Globalization;
using System.Security.Claims;
using PediAgenda.Nucleo;
using PediAgenda.Nucleo.Repositorios;

namespace PediAgenda.Api;

public static class BloqueioPeriodoRotas
{
    public static void MapearBloqueioPeriodo(this WebApplication app)
    {
        var rotas = app.MapGroup("/api/agenda/periodos")
            .RequireAuthorization(p => p.RequireRole("MEDICO", "RECEPCIONISTA"));

        // Recupera periodos bloqueados de verdade, sem usar a lista local.
        rotas.MapGet("/bloqueados", (
            int? medico,
            ClaimsPrincipal usuario,
            IAgendaRepositorio agendas) =>
        {
            if (!int.TryParse(usuario.FindFirst("sub")?.Value, out int idUsuario))
                return Results.Unauthorized();

            string? perfil = usuario.FindFirst("role")?.Value;
            int? idMedico = perfil switch
            {
                "MEDICO" => agendas.IdMedicoDoUsuario(idUsuario),
                "RECEPCIONISTA" => medico,
                _ => null
            };

            if (idMedico is null or <= 0)
                return Erro("MEDICO_INVALIDO", "Selecione um médico válido.", 400);

            var repo = new BloqueioPeriodoRepositorioMySql(Configuracao.StringDeConexao!);
            var bloqueios = repo.ListarBloqueados(idMedico.Value);

            return Results.Ok(bloqueios.Select(b => new
            {
                data = b.Data.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                inicio = b.Inicio.ToString("HH:mm", CultureInfo.InvariantCulture),
                fim = b.Fim.ToString("HH:mm", CultureInfo.InvariantCulture),
                quantidadeHorarios = b.QuantidadeHorarios
            }));
        });

        rotas.MapPost("/bloquear", (
            PeriodoAgendaRequisicao req,
            ClaimsPrincipal usuario,
            IAgendaRepositorio agendas) =>
            Executar(req, usuario, agendas, bloquear: true));

        rotas.MapPost("/liberar", (
            PeriodoAgendaRequisicao req,
            ClaimsPrincipal usuario,
            IAgendaRepositorio agendas) =>
            Executar(req, usuario, agendas, bloquear: false));
    }

    private static IResult Executar(
        PeriodoAgendaRequisicao req,
        ClaimsPrincipal usuario,
        IAgendaRepositorio agendas,
        bool bloquear)
    {
        string? perfil = usuario.FindFirst("role")?.Value;
        if (!int.TryParse(usuario.FindFirst("sub")?.Value, out int idUsuario))
            return Results.Unauthorized();

        int? idMedico;
        if (perfil == "MEDICO")
        {
            // Médico só pode modificar a própria agenda, nunca a de outro usuário.
            idMedico = agendas.IdMedicoDoUsuario(idUsuario);
        }
        else if (perfil == "RECEPCIONISTA")
        {
            idMedico = req.IdMedico;
        }
        else
        {
            return Results.Forbid();
        }

        if (idMedico is null or <= 0)
            return Erro("MEDICO_INVALIDO", "Selecione um médico válido.", 400);

        if (!DateOnly.TryParseExact(req.DataInicial, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataInicial) ||
            !DateOnly.TryParseExact(req.DataFinal, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataFinal) ||
            !TimeOnly.TryParseExact(req.HorarioInicial, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var horarioInicial) ||
            !TimeOnly.TryParseExact(req.HorarioFinal, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var horarioFinal))
        {
            return Erro("PERIODO_INVALIDO",
                "Confira as datas e os horários informados.", 400);
        }

        if (dataInicial.ToDateTime(horarioInicial) >= dataFinal.ToDateTime(horarioFinal))
            return Erro("PERIODO_INVALIDO", "A data e o horário finais devem ser posteriores ao início do período.", 400);

        try
        {
            var repositorio = new BloqueioPeriodoRepositorioMySql(
                Configuracao.StringDeConexao!);

            var resultado = bloquear
                ? repositorio.Bloquear(idMedico.Value, dataInicial, dataFinal,
                    horarioInicial, horarioFinal)
                : repositorio.Liberar(idMedico.Value, dataInicial, dataFinal,
                    horarioInicial, horarioFinal);

            return Results.Ok(new
            {
                idMedico,
                bloqueado = bloquear,
                horariosAlterados = resultado.HorariosAlterados,
                consultasCanceladas = resultado.ConsultasCanceladas
            });
        }
        catch (PeriodoAgendaException ex)
        {
            return Erro("PERIODO_INDISPONIVEL", ex.Message, 409);
        }
    }

    private static IResult Erro(string codigo, string mensagem, int status) =>
        Results.Json(new { codigo, mensagem }, statusCode: status);
}

public sealed record PeriodoAgendaRequisicao(
    int? IdMedico,
    string? DataInicial,
    string? DataFinal,
    string? HorarioInicial,
    string? HorarioFinal);
