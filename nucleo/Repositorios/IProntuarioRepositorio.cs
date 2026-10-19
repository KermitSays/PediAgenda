using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Repositorios;

public enum ResultadoRegistroProntuario
{
    Registrado, NaoEncontrada, JaRegistrado, Cancelada, AindaNaoComecou
}

public interface IProntuarioRepositorio
{
    IReadOnlyList<PacienteDoMedico> Pacientes(int idMedico);
    bool TemPaciente(int idMedico, int idPaciente);
    IReadOnlyList<Prontuario> Historico(int idMedico, int idPaciente);
    Prontuario? Buscar(int idMedico, int idConsulta);
    ResultadoRegistroProntuario Registrar(int idMedico, int idConsulta,
        string observacoes, DateTime agoraClinica, DateTime agoraUtc);
}
