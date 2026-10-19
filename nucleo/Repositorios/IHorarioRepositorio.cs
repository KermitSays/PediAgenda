using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public interface IHorarioRepositorio
{
    /// <summary>
    /// Horários livres de um dia a partir de uma hora, opcionalmente filtrados
    /// por especialidade e por médico.
    /// </summary>
    IReadOnlyList<HorarioLivre> ListarLivres(DateOnly data, TimeOnly aPartirDe,
                                             string? especialidade, int? idMedico);
}
