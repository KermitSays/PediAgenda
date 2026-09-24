using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public enum ResultadoBloqueio
{
    Alterado,
    NaoEncontrado,
    DeOutroMedico,
    TemConsulta
}

public enum ResultadoTransicao
{
    Alterada,
    NaoEncontrada,
    DeOutroMedico,
    StatusNaoPermite,
    AindaNaoComecou,
    JaComecou
}

/// <summary>
/// O lado da clínica: horários da agenda e o andamento das consultas.
/// Quando <c>somenteDoMedico</c> vem preenchido, a operação só vale se o
/// horário ou a consulta forem da agenda desse médico.
/// </summary>
public interface IAgendaRepositorio
{
    int? IdMedicoDoUsuario(int idUsuario);

    int? IdAgendaDoMedico(int idMedico);

    /// <summary>
    /// Grava os blocos que não se sobrepõem a nenhum horário daquele dia e
    /// devolve quantos foram criados. Os que se sobrepõem são pulados.
    /// </summary>
    int AbrirHorarios(int idAgenda, DateOnly data, IReadOnlyList<(TimeOnly Inicio, TimeOnly Fim)> blocos);

    IReadOnlyList<ItemAgenda> AgendaDoDia(DateOnly data, int? idMedico);

    ResultadoBloqueio AlterarDisponibilidade(int idHorario, int? somenteDoMedico, bool disponivel);

    ResultadoTransicao MudarStatusConsulta(int idConsulta, int? somenteDoMedico,
                                          IReadOnlySet<string> statusPermitidos, string novoStatus,
                                          DateTime agora, bool exigeJaTerComecado);
}
