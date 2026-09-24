using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

/// <summary>
/// Acesso aos pacientes. Tudo parte do responsável: não existe método que
/// liste ou busque pacientes sem dizer de quem são.
/// </summary>
public interface IPacienteRepositorio
{
    /// <summary>
    /// O id_responsavel do usuário logado, ou null se ele não for responsável.
    /// </summary>
    int? IdResponsavelDoUsuario(int idUsuario);

    IReadOnlyList<Paciente> ListarPorResponsavel(int idResponsavel);

    bool Existe(int idResponsavel, string nome, DateOnly dataNascimento);

    int Inserir(int idResponsavel, string nome, DateOnly dataNascimento);
}
