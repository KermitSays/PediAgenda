using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public enum ResultadoReserva
{
    Reservada,
    PacienteNaoEncontrado,
    HorarioNaoEncontrado,
    HorarioPassado,
    HorarioBloqueado,
    HorarioOcupado,
    PacienteOcupadoNoHorario
}

public enum ResultadoCancelamento
{
    Cancelada,
    NaoEncontrada,
    JaEncerrada,
    JaPassou
}

/// <summary>
/// Acesso às consultas. Todo método recebe o responsável: uma família só
/// enxerga, reserva e cancela consultas dos próprios filhos.
/// </summary>
public interface IConsultaRepositorio
{
    /// <summary>
    /// Confere e reserva numa única transação, com o horário travado: se dois
    /// pedidos chegam para o mesmo horário ao mesmo tempo, só um consegue.
    /// </summary>
    (ResultadoReserva Resultado, int IdConsulta) Reservar(
        int idResponsavel, int idPaciente, int idHorario, string tipoAtendimento, DateTime agora);

    IReadOnlyList<ConsultaDetalhe> ListarDoResponsavel(int idResponsavel);

    ConsultaDetalhe? BuscarDoResponsavel(int idResponsavel, int idConsulta);

    ResultadoCancelamento Cancelar(int idResponsavel, int idConsulta, DateTime agora);
}
