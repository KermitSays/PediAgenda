
using System.Collections.ObjectModel;
using ClosedXML.Excel;
using CommunityToolkit.Maui.Storage;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class RelatoriosMedico : ContentPage
{
    public ObservableCollection<RelatorioConsultaItem> Resultados
    { get; set; } = new();

    public RelatoriosMedico()
    {
        InitializeComponent();

        BindingContext = this;

        ConfigurarDatas();
        BuscarAtendimentos();
    }

    // ============================================
    // CONFIGURAR PERÍODO INICIAL
    // ============================================

    private void ConfigurarDatas()
    {
        if (ConsultasMedicoDados.Consultas.Count > 0)
        {
            DataInicialPicker.Date =
                ConsultasMedicoDados.Consultas
                    .Min(c => c.Data);

            DataFinalPicker.Date =
                ConsultasMedicoDados.Consultas
                    .Max(c => c.Data);
        }
        else
        {
            DataInicialPicker.Date =
                DateTime.Today.AddMonths(-1);

            DataFinalPicker.Date = DateTime.Today;
        }
    }

    // ============================================
    // BOTÃO BUSCAR
    // ============================================

    private async void BuscarButton_Clicked(
        object sender,
        EventArgs e)
    {
        DateTime dataInicial =
            DataInicialPicker.Date ?? DateTime.Today;

        DateTime dataFinal =
            DataFinalPicker.Date ?? DateTime.Today;

        if (dataFinal.Date < dataInicial.Date)
        {
            await DisplayAlertAsync(
                "Período inválido",
                "A data final não pode ser anterior à data inicial.",
                "OK");

            return;
        }

        BuscarAtendimentos();
    }

    // ============================================
    // BUSCAR CONSULTAS REALIZADAS
    // ============================================

    private void BuscarAtendimentos()
    {
        DateTime dataInicial =
            DataInicialPicker.Date ?? DateTime.Today;

        DateTime dataFinal =
            DataFinalPicker.Date ?? DateTime.Today;

        // Mantém somente consultas realizadas.
        var consultasFiltradas =
            ConsultasMedicoDados.Consultas
                .Where(c =>
                    c.Data.Date >= dataInicial.Date &&
                    c.Data.Date <= dataFinal.Date &&
                    string.Equals(
                        c.Status,
                        "Realizada",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Data)
                .ThenBy(c => c.Horario)
                .ToList();

        Resultados.Clear();

        foreach (var consulta in consultasFiltradas)
        {
            Resultados.Add(new RelatorioConsultaItem
            {
                Paciente = consulta.Paciente,
                Data = consulta.Data,
                Horario = consulta.Horario,
                Status = consulta.Status
            });
        }

        AtualizarResumo();
    }

    // ============================================
    // ATUALIZAR TOTAL
    // ============================================

    private void AtualizarResumo()
    {
        TotalRealizadasLabel.Text =
            Resultados.Count.ToString();
    }

    // ============================================
    // GERAR RELATÓRIO EM EXCEL
    // ============================================

    private async void GerarExcelButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (Resultados.Count == 0)
        {
            await DisplayAlertAsync(
                "Relatórios",
                "Não existem consultas realizadas para gerar o relatório.",
                "OK");

            return;
        }

        try
        {
            using var workbook = new XLWorkbook();

            var planilha =
                workbook.Worksheets.Add("Atendimentos");

            // ====================================
            // CABEÇALHO
            // ====================================

            planilha.Cell("A1").Value = "PediAgenda";

            planilha.Cell("A2").Value =
                "Relatório de Atendimentos Realizados";

            planilha.Range("A1:C1").Merge();
            planilha.Range("A2:C2").Merge();

            planilha.Cell("A1").Style.Font.Bold = true;
            planilha.Cell("A1").Style.Font.FontSize = 18;

            planilha.Cell("A2").Style.Font.Bold = true;
            planilha.Cell("A2").Style.Font.FontSize = 14;

            // ====================================
            // PERÍODO UTILIZADO
            // ====================================

            DateTime dataInicial =
                DataInicialPicker.Date ?? DateTime.Today;

            DateTime dataFinal =
                DataFinalPicker.Date ?? DateTime.Today;

            planilha.Cell("A4").Value = "Período:";

            planilha.Cell("B4").Value =
                $"{dataInicial:dd/MM/yyyy} até " +
                $"{dataFinal:dd/MM/yyyy}";

            // ====================================
            // RESUMO
            // ====================================

            planilha.Cell("A6").Value =
                "Total de consultas realizadas";

            planilha.Cell("B6").Value =
                Resultados.Count;

            planilha.Cell("A6").Style.Font.Bold = true;

            // ====================================
            // CABEÇALHO DA TABELA
            // ====================================

            int linhaCabecalho = 8;

            planilha.Cell(linhaCabecalho, 1).Value =
                "Paciente";

            planilha.Cell(linhaCabecalho, 2).Value =
                "Data";

            planilha.Cell(linhaCabecalho, 3).Value =
                "Horário";

            var cabecalho = planilha.Range(
                linhaCabecalho,
                1,
                linhaCabecalho,
                3);

            cabecalho.Style.Font.Bold = true;

            // ====================================
            // CONSULTAS REALIZADAS
            // ====================================

            int linhaAtual = linhaCabecalho + 1;

            foreach (var consulta in Resultados)
            {
                planilha.Cell(linhaAtual, 1).Value =
                    consulta.Paciente;

                planilha.Cell(linhaAtual, 2).Value =
                    consulta.Data;

                planilha.Cell(linhaAtual, 2)
                    .Style.DateFormat.Format =
                    "dd/MM/yyyy";

                planilha.Cell(linhaAtual, 3).Value =
                    consulta.Horario.ToString(@"hh\:mm");

                linhaAtual++;
            }

            // ====================================
            // AJUSTAR COLUNAS
            // ====================================

            planilha.Columns().AdjustToContents();

            // ====================================
            // SALVAR ARQUIVO
            // ====================================

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            string nomeArquivo =
                $"Relatorio_PediAgenda_" +
                $"{DateTime.Now:yyyyMMdd_HHmm}.xlsx";

            var resultado = await FileSaver.Default.SaveAsync(
                nomeArquivo,
                stream);

            if (resultado.IsSuccessful)
            {
                await DisplayAlertAsync(
                    "Relatório gerado",
                    "O arquivo Excel foi salvo com sucesso.",
                    "OK");
            }
            else
            {
                await DisplayAlertAsync(
                    "Relatórios",
                    "O arquivo não foi salvo.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro",
                "Não foi possível gerar o arquivo Excel.\n\n" +
                ex.Message,
                "OK");
        }
    }

    // ============================================
    // VOLTAR
    // ============================================

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

// ============================================
// MODELO DE EXIBIÇÃO
// ============================================

public class RelatorioConsultaItem
{
    public string Paciente { get; set; } =
        string.Empty;

    public DateTime Data { get; set; }

    public TimeSpan Horario { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string DataHoraFormatada =>
        $"{Data:dd/MM/yyyy} • {Horario:hh\\:mm}";
}
