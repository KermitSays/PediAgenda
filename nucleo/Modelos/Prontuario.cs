namespace PediAgenda.Nucleo.Modelos;

public record Prontuario(int Id, int IdConsulta, int IdPaciente,
    string Observacoes, DateTime DataRegistro);

public record PacienteDoMedico(int Id, string Nome, DateOnly DataNascimento,
    string Responsavel);
