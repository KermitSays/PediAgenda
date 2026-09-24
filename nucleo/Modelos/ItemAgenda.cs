namespace PediAgenda.Nucleo.Modelos;

/// <summary>
/// Quem está fazendo a chamada, tirado do token: o id do usuário e o perfil.
/// </summary>
public record Ator(int IdUsuario, string Perfil)
{
    public bool EhMedico => Perfil == "MEDICO";
    public bool EhRecepcionista => Perfil == "RECEPCIONISTA";
}

/// <summary>
/// Um bloco da agenda do dia, como o médico e a recepção enxergam: livre,
/// bloqueado ou ocupado — e, se ocupado, por quem.
/// </summary>
public record ItemAgenda(
    int IdHorario, DateOnly Data, TimeOnly Inicio, TimeOnly Fim, bool Disponivel,
    int IdMedico, string NomeMedico, string Especialidade,
    int? IdConsulta, string? StatusConsulta, string? TipoAtendimento,
    int? IdPaciente, string? NomePaciente, DateOnly? NascimentoPaciente,
    string? NomeResponsavel, string? TelefoneResponsavel)
{
    public string Situacao =>
        IdConsulta is not null ? "OCUPADO" : Disponivel ? "LIVRE" : "BLOQUEADO";
}
