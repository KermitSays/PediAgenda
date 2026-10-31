using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Api;

public static class RecepcaoAgendamentoRotas
{
    public static void MapearAgendamentosRecepcao(this WebApplication app)
    {
        // Apenas recepcionistas autenticados podem reservar consultas por pacientes.
        app.MapPost("/api/recepcao/consultas", (
            AgendamentoRecepcaoRequisicao pedido,
            RecepcaoAgendamentoRepositorioMySql pacientes,
            IConsultaRepositorio consultas,
            TimeProvider relogio) =>
        {
            if (pedido.IdPaciente is null or <= 0 || pedido.IdHorario is null or <= 0)
            {
                return Results.BadRequest(new
                {
                    codigo = "DADOS_INVALIDOS",
                    mensagem = "Selecione um paciente e um horário válidos."
                });
            }

            // Não confiar em um ID de responsável enviado pelo aplicativo.
            var idResponsavel = pacientes.IdResponsavelDoPaciente(pedido.IdPaciente.Value);
            if (idResponsavel is null)
                return Results.NotFound(new { codigo = "PACIENTE_NAO_ENCONTRADO", mensagem = "Paciente não encontrado." });

            // Reutiliza exatamente a reserva transacional do Responsável:
            // impede sobreposição, horário bloqueado, passado e dupla marcação.
            var agora = relogio.GetLocalNow().DateTime;

            var (resultado, idConsulta) = consultas.Reservar(
                idResponsavel.Value,
                pedido.IdPaciente.Value,
                pedido.IdHorario.Value,
                agora);


            IResult resposta = resultado switch
            {
                ResultadoReserva.Reservada => Results.Json(new
                {
                    id = idConsulta,
                    status = StatusConsulta.Agendada,
                    mensagem = "Consulta agendada com sucesso."
                }, statusCode: StatusCodes.Status201Created),

                ResultadoReserva.PacienteNaoEncontrado => Results.NotFound(new
                {
                    codigo = "PACIENTE_NAO_ENCONTRADO",
                    mensagem = "Paciente não encontrado."
                }),

                ResultadoReserva.HorarioNaoEncontrado => Results.NotFound(new
                {
                    codigo = "HORARIO_NAO_ENCONTRADO",
                    mensagem = "Horário não encontrado."
                }),

                ResultadoReserva.HorarioPassado => Results.Conflict(new
                {
                    codigo = "HORARIO_PASSADO",
                    mensagem = "O horário selecionado já passou."
                }),

                ResultadoReserva.HorarioBloqueado or ResultadoReserva.HorarioOcupado or
                ResultadoReserva.PacienteOcupadoNoHorario => Results.Conflict(new
                {
                    codigo = "HORARIO_INDISPONIVEL",
                    mensagem = "O horário está indisponível ou o paciente já possui uma consulta nesse período."
                }),

                _ => Results.Problem("Não foi possível concluir o agendamento.")
            };
            return resposta;
        }).RequireAuthorization(p => p.RequireRole("RECEPCIONISTA"));
    }
}

public sealed record AgendamentoRecepcaoRequisicao(int? IdPaciente, int? IdHorario);
