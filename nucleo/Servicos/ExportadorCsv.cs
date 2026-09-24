using System.Globalization;
using System.Text;
using PediAgenda.Nucleo.Modelos;

namespace PediAgenda.Nucleo.Servicos;

/// <summary>
/// Exporta o relatório em CSV (RF11), no jeito que o Excel em português abre
/// direto: separador ponto e vírgula, datas dd/MM/aaaa e UTF-8 com BOM, para
/// os acentos aparecerem certos.
/// </summary>
public static class ExportadorCsv
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static byte[] Gerar(RelatorioCompleto relatorio)
    {
        var c = relatorio.Cabecalho;
        var r = relatorio.Resumo;
        var csv = new StringBuilder();

        Linha(csv, "Relatório de atendimentos", c.NomeMedico, c.Especialidade);
        Linha(csv, "Período", $"{Data(c.Inicio)} a {Data(c.Fim)}");
        Linha(csv, "Gerado em", c.GeradoEm.ToString("dd/MM/yyyy HH:mm", Br));
        csv.AppendLine();

        Linha(csv, "Data", "Hora", "Paciente", "Idade", "Tipo de atendimento", "Status");
        foreach (var consulta in relatorio.Consultas)
            Linha(csv, Data(consulta.Data), consulta.Inicio.ToString("HH:mm"), consulta.NomePaciente,
                  consulta.IdadePaciente.ToString(), Tipo(consulta.TipoAtendimento), Status(consulta.Status));
        csv.AppendLine();

        Linha(csv, "Resumo");
        Linha(csv, "Total de consultas", r.TotalConsultas.ToString());
        Linha(csv, "Agendadas", r.Agendadas.ToString());
        Linha(csv, "Confirmadas", r.Confirmadas.ToString());
        Linha(csv, "Realizadas", r.Realizadas.ToString());
        Linha(csv, "Canceladas", r.Canceladas.ToString());
        Linha(csv, "Convênio", r.Convenio.ToString());
        Linha(csv, "Particular", r.Particular.ToString());
        Linha(csv, "Horários ofertados", r.HorariosOfertados.ToString());
        Linha(csv, "Horários bloqueados", r.HorariosBloqueados.ToString());
        Linha(csv, "Taxa de ocupação (%)", r.TaxaOcupacao?.ToString("0.0", Br) ?? "-");
        Linha(csv, "Taxa de cancelamento (%)", r.TaxaCancelamento?.ToString("0.0", Br) ?? "-");

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public static string NomeDoArquivo(RelatorioGerado c) =>
        $"relatorio-{c.Id}-{c.Inicio:yyyy-MM-dd}-a-{c.Fim:yyyy-MM-dd}.csv";

    private static void Linha(StringBuilder csv, params string[] campos) =>
        csv.AppendLine(string.Join(";", campos.Select(Campo)));

    private static string Campo(string valor)
    {
        // Proteção contra injeção de fórmula: o nome do paciente é digitado
        // pela família, e um texto começando com = + - @ o Excel executaria
        // como fórmula ao abrir o arquivo. O apóstrofo faz ele virar texto.
        if (valor.Length > 0 && "=+-@".Contains(valor[0]))
            valor = "'" + valor;

        return valor.IndexOfAny([';', '"', '\n', '\r']) >= 0
            ? "\"" + valor.Replace("\"", "\"\"") + "\""
            : valor;
    }

    private static string Data(DateOnly data) => data.ToString("dd/MM/yyyy", Br);

    private static string Tipo(string tipo) => tipo == TipoAtendimento.Convenio ? "Convênio" : "Particular";

    private static string Status(string status) => status switch
    {
        StatusConsulta.Agendada => "Agendada",
        StatusConsulta.Confirmada => "Confirmada",
        StatusConsulta.Realizada => "Realizada",
        StatusConsulta.Cancelada => "Cancelada",
        _ => status
    };
}
