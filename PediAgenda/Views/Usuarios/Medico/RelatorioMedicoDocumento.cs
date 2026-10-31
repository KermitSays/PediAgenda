using System.Globalization;
using ClosedXML.Excel;

namespace PediAgenda.Views.Usuarios.Medico;

public sealed record MedicoRelatorioApi(int Id, string Nome, string Especialidade);
public sealed record LinhaRelatorioMedicoApi(DateOnly Data, string Inicio, string Paciente,
    int Idade, string Status)
{
    public string DataHoraFormatada => $"{Data:dd/MM/yyyy} • {Inicio}";
}
public sealed record RelatorioMedicoResposta(int Id, MedicoRelatorioApi Medico,
    DateOnly Inicio, DateOnly Fim, IReadOnlyList<LinhaRelatorioMedicoApi> Consultas);

// Período e registros pertencem à mesma busca concluída.
public sealed record RelatorioMedicoDocumento(MedicoRelatorioApi Medico, DateOnly Inicio,
    DateOnly Fim, IReadOnlyList<LinhaRelatorioMedicoApi> Atendimentos)
{
    public static RelatorioMedicoDocumento Criar(RelatorioMedicoResposta resposta,
        DateOnly inicio, DateOnly fim)
    {
        if (resposta.Inicio != inicio || resposta.Fim != fim || fim < inicio)
            throw new InvalidOperationException("O período retornado não corresponde à busca.");
        var linhas = resposta.Consultas.Where(c => c.Status == "REALIZADA")
            .OrderBy(c => c.Data).ThenBy(c => c.Inicio).ToArray();
        if (linhas.Any(c => c.Data < inicio || c.Data > fim ||
            !TimeOnly.TryParseExact(c.Inicio, "HH:mm", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _)))
            throw new InvalidOperationException("O relatório retornou atendimentos inválidos.");
        return new(resposta.Medico, inicio, fim, Array.AsReadOnly(linhas));
    }
}

public static class ExportadorExcelMedico
{
    public static MemoryStream Gerar(RelatorioMedicoDocumento documento)
    {
        using var workbook = new XLWorkbook();
        var folha = workbook.Worksheets.Add("Atendimentos");
        folha.Cell("A1").Value = "PediAgenda";
        folha.Cell("A2").Value = "Relatório de Atendimentos Realizados";
        folha.Range("A1:C1").Merge();
        folha.Range("A2:C2").Merge();
        folha.Range("A1:A2").Style.Font.Bold = true;
        folha.Cell("A1").Style.Font.FontSize = 18;
        folha.Cell("A3").Value = "Médico:";
        folha.Cell("B3").Value = documento.Medico.Nome;
        folha.Cell("A4").Value = "Período:";
        folha.Cell("B4").Value = $"{documento.Inicio:dd/MM/yyyy} até {documento.Fim:dd/MM/yyyy}";
        folha.Cell("A6").Value = "Total de consultas realizadas";
        folha.Cell("B6").Value = documento.Atendimentos.Count;
        folha.Cell("A8").Value = "Paciente";
        folha.Cell("B8").Value = "Data";
        folha.Cell("C8").Value = "Horário";
        folha.Range("A8:C8").Style.Font.Bold = true;
        int linha = 9;
        foreach (var item in documento.Atendimentos)
        {
            folha.Cell(linha, 1).Value = item.Paciente;
            folha.Cell(linha, 2).Value = item.Data.ToDateTime(TimeOnly.MinValue);
            folha.Cell(linha, 2).Style.DateFormat.Format = "dd/MM/yyyy";
            folha.Cell(linha, 3).Value = item.Inicio;
            linha++;
        }
        // Larguras fixas dispensam fontes instaladas para gerar o Excel no Android.
        folha.Column(1).Width = 38;
        folha.Column(2).Width = 34;
        folha.Column(3).Width = 14;
        var stream = new MemoryStream();
        try { workbook.SaveAs(stream); stream.Position = 0; return stream; }
        catch { stream.Dispose(); throw; }
    }
}
