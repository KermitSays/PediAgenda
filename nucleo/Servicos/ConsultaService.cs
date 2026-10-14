using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;

namespace PediAgenda.Nucleo.Servicos;

public enum MotivoConsulta
{
    Nenhum,
    NaoEResponsavel,
    DadosInvalidos,
    PacienteNaoEncontrado,
    HorarioNaoEncontrado,
    ConsultaNaoEncontrada,
    HorarioIndisponivel,
    PacienteOcupadoNoHorario,
    NaoCancelavel
}

public record ResultadoConsulta(bool Sucesso, string Mensagem, MotivoConsulta Motivo,
                                ConsultaDetalhe? Consulta = null,
                                IReadOnlyDictionary<string, string>? Campos = null)
{
    public static ResultadoConsulta Falha(MotivoConsulta motivo, string mensagem) => new(false, mensagem, motivo);
}

/// <summary>
/// Agendamento e cancelamento de consultas pelo responsável (RF05, RF07).
///
/// Como em pacientes, o responsável vem do token. A garantia de que dois
/// pedidos não ficam com o mesmo horário está no repositório, dentro de uma
/// transação com o horário travado.
/// </summary>
public class ConsultaService(IConsultaRepositorio consultas, IPacienteRepositorio pacientes,
                             TimeProvider? relogio = null)
{
    private readonly IConsultaRepositorio _consultas = consultas;
    private readonly IPacienteRepositorio _pacientes = pacientes;
    private readonly TimeProvider _relogio = relogio ?? TimeProvider.System;

    private DateTime Agora => _relogio.GetLocalNow().DateTime;

    public ResultadoConsulta Agendar(int idUsuario, int? idPaciente, int? idHorario, string? tipoInformado)
    {
        var idResponsavel = _pacientes.IdResponsavelDoUsuario(idUsuario);
        if (idResponsavel is null)
            return NaoEResponsavel();

        var tipo = (tipoInformado ?? "").Trim().ToUpperInvariant();
        var campos = new Dictionary<string, string>();

        if (idPaciente is null or <= 0)
            campos["idPaciente"] = "Escolha o paciente.";
        if (idHorario is null or <= 0)
            campos["idHorario"] = "Escolha o horário.";
        if (!TipoAtendimento.Validos.Contains(tipo))
            campos["tipoAtendimento"] = "Escolha CONVENIO ou PARTICULAR.";

        if (campos.Count > 0)
            return new ResultadoConsulta(false, "Verifique os campos destacados.",
                                         MotivoConsulta.DadosInvalidos, Campos: campos);

        var (resultado, idConsulta) = _consultas.Reservar(
            idResponsavel.Value, idPaciente!.Value, idHorario!.Value, tipo, Agora);

        return resultado switch
        {
            ResultadoReserva.Reservada => new ResultadoConsulta(true, "Consulta agendada.", MotivoConsulta.Nenhum,
                _consultas.BuscarDoResponsavel(idResponsavel.Value, idConsulta)),
            ResultadoReserva.PacienteNaoEncontrado => ResultadoConsulta.Falha(
                MotivoConsulta.PacienteNaoEncontrado, "Paciente não encontrado."),
            ResultadoReserva.HorarioNaoEncontrado => ResultadoConsulta.Falha(
                MotivoConsulta.HorarioNaoEncontrado, "Horário não encontrado."),
            ResultadoReserva.HorarioPassado => ResultadoConsulta.Falha(
                MotivoConsulta.HorarioIndisponivel, "Esse horário já passou. Escolha outro."),
            ResultadoReserva.HorarioBloqueado => ResultadoConsulta.Falha(
                MotivoConsulta.HorarioIndisponivel, "Esse horário não está disponível. Escolha outro."),
            ResultadoReserva.HorarioOcupado => ResultadoConsulta.Falha(
                MotivoConsulta.HorarioIndisponivel, "Esse horário acabou de ser reservado. Escolha outro."),
            ResultadoReserva.PacienteOcupadoNoHorario => ResultadoConsulta.Falha(
                MotivoConsulta.PacienteOcupadoNoHorario, "Esse paciente já tem consulta nesse horário."),
            _ => throw new InvalidOperationException($"Resultado inesperado: {resultado}")
        };
    }

    /// <summary>
    /// As consultas dos filhos do responsável, ou null se o usuário não for responsável.
    /// </summary>
    public IReadOnlyList<ConsultaDetalhe>? Listar(int idUsuario)
    {
        var idResponsavel = _pacientes.IdResponsavelDoUsuario(idUsuario);
        return idResponsavel is null ? null : _consultas.ListarDoResponsavel(idResponsavel.Value);
    }

    public ResultadoConsulta Cancelar(int idUsuario, int idConsulta)
    {
        var idResponsavel = _pacientes.IdResponsavelDoUsuario(idUsuario);
        if (idResponsavel is null)
            return NaoEResponsavel();

        return _consultas.Cancelar(idResponsavel.Value, idConsulta, Agora) switch
        {
            ResultadoCancelamento.Cancelada => new ResultadoConsulta(true, "Consulta cancelada.", MotivoConsulta.Nenhum,
                _consultas.BuscarDoResponsavel(idResponsavel.Value, idConsulta)),
            ResultadoCancelamento.NaoEncontrada => ResultadoConsulta.Falha(
                MotivoConsulta.ConsultaNaoEncontrada, "Consulta não encontrada."),
            ResultadoCancelamento.JaEncerrada => ResultadoConsulta.Falha(
                MotivoConsulta.NaoCancelavel, "Essa consulta já foi cancelada ou realizada."),
            ResultadoCancelamento.JaPassou => ResultadoConsulta.Falha(
                MotivoConsulta.NaoCancelavel, "Não dá para cancelar uma consulta que já começou."),
            var outro => throw new InvalidOperationException($"Resultado inesperado: {outro}")
        };
    }

    private static ResultadoConsulta NaoEResponsavel() =>
        ResultadoConsulta.Falha(MotivoConsulta.NaoEResponsavel, "Só o responsável pode agendar consultas.");
}
