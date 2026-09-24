using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;

namespace PediAgenda.Nucleo.Servicos;

/// <summary>
/// Relatórios de atendimento por período e por médico (RF10, RNF05).
///
/// Cada relatório gerado fica registrado em `relatorio_atendimento` — é o
/// histórico. Reabrir um relatório recalcula os números a partir do banco:
/// mostra o período salvo com os dados de agora, e não uma cópia congelada.
///
/// Mesma regra de acesso da agenda: o médico vê só os próprios relatórios, a
/// recepção vê os de todos.
/// </summary>
public class RelatorioService(IRelatorioRepositorio relatorios, IAgendaRepositorio agenda)
{
    // Período máximo de um relatório: um ano. Mantém a geração rápida (RNF05).
    public const int DiasMaximos = 366;

    private readonly IRelatorioRepositorio _relatorios = relatorios;
    private readonly IAgendaRepositorio _agenda = agenda;

    public ResultadoClinica<RelatorioCompleto> Gerar(Ator ator, string? inicioInformado, string? fimInformado,
                                                     int? idMedicoInformado)
    {
        var campos = new Dictionary<string, string>();

        int? idMedico;
        if (ator.EhMedico)
        {
            idMedico = _agenda.IdMedicoDoUsuario(ator.IdUsuario);
            if (idMedico is null)
                return SemMedico<RelatorioCompleto>();
        }
        else
        {
            idMedico = idMedicoInformado;
            if (idMedico is null or <= 0)
                campos["idMedico"] = "Informe o médico.";
        }

        var inicio = Entrada.LerData(inicioInformado, "inicio", campos);
        var fim = Entrada.LerData(fimInformado, "fim", campos);
        if (inicio is not null && fim is not null)
        {
            if (fim < inicio)
                campos["fim"] = "O fim do período precisa ser igual ou depois do início.";
            else if (fim.Value.DayNumber - inicio.Value.DayNumber + 1 > DiasMaximos)
                campos["fim"] = $"O período pode ter no máximo {DiasMaximos} dias.";
        }

        if (campos.Count > 0)
            return ResultadoClinica<RelatorioCompleto>.Invalido(campos);

        if (_agenda.IdAgendaDoMedico(idMedico!.Value) is null)
            return ResultadoClinica<RelatorioCompleto>.Erro(TipoFalhaClinica.NaoEncontrado,
                "MEDICO_NAO_ENCONTRADO", "Médico não encontrado.");

        var id = _relatorios.Registrar(idMedico.Value, inicio!.Value, fim!.Value);
        return ResultadoClinica<RelatorioCompleto>.Ok(Montar(_relatorios.Buscar(id)!));
    }

    public ResultadoClinica<IReadOnlyList<RelatorioGerado>> Historico(Ator ator)
    {
        int? idMedico = null;
        if (ator.EhMedico)
        {
            idMedico = _agenda.IdMedicoDoUsuario(ator.IdUsuario);
            if (idMedico is null)
                return SemMedico<IReadOnlyList<RelatorioGerado>>();
        }
        return ResultadoClinica<IReadOnlyList<RelatorioGerado>>.Ok(_relatorios.Listar(idMedico));
    }

    public ResultadoClinica<RelatorioCompleto> Abrir(Ator ator, int idRelatorio)
    {
        var cabecalho = _relatorios.Buscar(idRelatorio);
        if (cabecalho is null)
            return ResultadoClinica<RelatorioCompleto>.Erro(TipoFalhaClinica.NaoEncontrado,
                "RELATORIO_NAO_ENCONTRADO", "Relatório não encontrado.");

        if (ator.EhMedico && _agenda.IdMedicoDoUsuario(ator.IdUsuario) != cabecalho.IdMedico)
            return ResultadoClinica<RelatorioCompleto>.Erro(TipoFalhaClinica.SemPermissao,
                "SEM_PERMISSAO", "Esse relatório é de outro médico.");

        return ResultadoClinica<RelatorioCompleto>.Ok(Montar(cabecalho));
    }

    private RelatorioCompleto Montar(RelatorioGerado cabecalho)
    {
        var consultas = _relatorios.Consultas(cabecalho.IdMedico, cabecalho.Inicio, cabecalho.Fim);
        var (ofertados, bloqueados) = _relatorios.Horarios(cabecalho.IdMedico, cabecalho.Inicio, cabecalho.Fim);

        int Contar(string status) => consultas.Count(c => c.Status == status);
        var canceladas = Contar(StatusConsulta.Cancelada);
        var ativas = consultas.Count - canceladas;
        var abertos = ofertados - bloqueados;

        var resumo = new ResumoRelatorio(
            TotalConsultas: consultas.Count,
            Agendadas: Contar(StatusConsulta.Agendada),
            Confirmadas: Contar(StatusConsulta.Confirmada),
            Realizadas: Contar(StatusConsulta.Realizada),
            Canceladas: canceladas,
            Convenio: consultas.Count(c => c.TipoAtendimento == TipoAtendimento.Convenio),
            Particular: consultas.Count(c => c.TipoAtendimento == TipoAtendimento.Particular),
            HorariosOfertados: ofertados,
            HorariosBloqueados: bloqueados,
            // Percentuais com uma casa; null quando não há base para calcular.
            TaxaOcupacao: abertos > 0 ? Math.Round(100m * ativas / abertos, 1) : null,
            TaxaCancelamento: consultas.Count > 0 ? Math.Round(100m * canceladas / consultas.Count, 1) : null);

        return new RelatorioCompleto(cabecalho, resumo, consultas);
    }

    private static ResultadoClinica<T> SemMedico<T>() =>
        ResultadoClinica<T>.Erro(TipoFalhaClinica.SemPermissao, "SEM_PERMISSAO",
                                 "Seu usuário não está ligado a um médico.");
}
