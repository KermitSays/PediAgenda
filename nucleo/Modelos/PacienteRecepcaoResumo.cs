
namespace PediAgenda.Nucleo.Modelos;

public record PacienteRecepcaoResumo(
    int Id,
    string Nome,
    DateOnly DataNascimento,
    string Responsavel,
    string TelefoneResponsavel,
    string? EmailResponsavel
);
