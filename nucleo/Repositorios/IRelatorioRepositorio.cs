using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public interface IRelatorioRepositorio
{
    int Registrar(int idMedico, DateOnly inicio, DateOnly fim);

    RelatorioGerado? Buscar(int idRelatorio);

    /// <summary>Histórico, do mais recente para o mais antigo; de um médico ou de todos.</summary>
    IReadOnlyList<RelatorioGerado> Listar(int? idMedico);

    IReadOnlyList<LinhaRelatorio> Consultas(int idMedico, DateOnly inicio, DateOnly fim);

    (int Ofertados, int Bloqueados) Horarios(int idMedico, DateOnly inicio, DateOnly fim);
}
