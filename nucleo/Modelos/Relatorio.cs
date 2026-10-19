namespace PediAgenda.Nucleo.Modelos;

/// <summary>
/// Um relatório registrado na tabela `relatorio_atendimento`: de qual médico,
/// de que período e quando foi gerado. É o histórico.
/// </summary>
public record RelatorioGerado(int Id, int IdMedico, string NomeMedico, string Especialidade,
                              DateOnly Inicio, DateOnly Fim, DateTime GeradoEm);

/// <summary>Uma consulta do período, como aparece no relatório.</summary>
public record LinhaRelatorio(DateOnly Data, TimeOnly Inicio, string NomePaciente, DateOnly NascimentoPaciente,
                             string TipoAtendimento, string Status)
{
    public int IdadePaciente => new Paciente { DataNascimento = NascimentoPaciente }.IdadeEm(Data);
}

public record ResumoRelatorio(
    int TotalConsultas, int Agendadas, int Confirmadas, int Realizadas, int Canceladas,
    int Convenio, int Particular,
    int HorariosOfertados, int HorariosBloqueados,
    decimal? TaxaOcupacao, decimal? TaxaCancelamento);

public record RelatorioCompleto(RelatorioGerado Cabecalho, ResumoRelatorio Resumo,
                                IReadOnlyList<LinhaRelatorio> Consultas);
