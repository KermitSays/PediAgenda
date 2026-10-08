using System.Collections.ObjectModel;
using ClosedXML.Excel;
using CommunityToolkit.Maui.Storage;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class RelatoriosMedico : ContentPage
{
    public ObservableCollection<RelatorioConsultaItem> Resultados { get; set; } = new();

    public RelatoriosMedico()
    {
        InitializeComponent();

        BindingContext = this;

        ConfigurarDatas();
        CarregarPacientes();
        CarregarStatus();
        BuscarAtendimentos();
    }


    // Configura automaticamente o período
    // de acordo com os dados mockados disponíveis.
    private void ConfigurarDatas()
    {
        if (ConsultasMedicoDados.Consultas.Count > 0)
        {
            DataInicialPicker.Date =
                ConsultasMedicoDados.Consultas.Min(c => c.Data);

            DataFinalPicker.Date =
                ConsultasMedicoDados.Consultas.Max(c => c.Data);
        }
        else
        {
            DataInicialPicker.Date = DateTime.Today.AddMonths(-1);
            DataFinalPicker.Date = DateTime.Today;
        }
    }


    // Carrega os pacientes existentes nos mocks.
    private void CarregarPacientes()
    {
        var pacientes = ConsultasMedicoDados.Consultas
            .Select(c => c.Paciente)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        PacientePicker.Items.Clear();

        PacientePicker.Items.Add("Todos");

        foreach (var paciente in pacientes)
        {
            PacientePicker.Items.Add(paciente);
        }

        PacientePicker.SelectedIndex = 0;
    }


    // Status utilizados atualmente nas consultas mockadas.
    private void CarregarStatus()
    {
        StatusPicker.Items.Clear();

        StatusPicker.Items.Add("Todos");
        StatusPicker.Items.Add("Confirmada");
        StatusPicker.Items.Add("Cancelada");
        StatusPicker.Items.Add("Por confirmar");
        StatusPicker.Items.Add("Realizada");

        StatusPicker.SelectedIndex = 0;
    }


    private void BuscarButton_Clicked(object sender, EventArgs e)
    {
        BuscarAtendimentos();
    }


    private async void BuscarAtendimentos()
    {
        DateTime dataInicial = DataInicialPicker.Date ?? DateTime.Today;
        DateTime dataFinal = DataFinalPicker.Date ?? DateTime.Today;

        if (dataFinal < dataInicial)
        {
            await DisplayAlertAsync(
                "Período inválido",
                "A data final não pode ser anterior à data inicial.",
                "OK");

            return;
        }


        string pacienteSelecionado =
            PacientePicker.SelectedItem?.ToString() ?? "Todos";

        string statusSelecionado =
            StatusPicker.SelectedItem?.ToString() ?? "Todos";


        var consultasFiltradas =
            ConsultasMedicoDados.Consultas
                .Where(c =>
                    c.Data.Date >= dataInicial.Date &&
                    c.Data.Date <= dataFinal.Date)
                .Where(c =>
                    pacienteSelecionado == "Todos" ||
                    c.Paciente == pacienteSelecionado)
                .Where(c =>
                    statusSelecionado == "Todos" ||
                    c.Status == statusSelecionado)
                .OrderBy(c => c.Data)
                .ThenBy(c => c.Horario)
                .ToList();


        Resultados.Clear();


        foreach (var consulta in consultasFiltradas)
        {
            Resultados.Add(
                new RelatorioConsultaItem
                {
                    Paciente = consulta.Paciente,
                    Data = consulta.Data,
                    Horario = consulta.Horario,
                    Status = consulta.Status
                });
        }


        AtualizarResumo();
    }


    // Atualiza os quatro cards de resumo.
    private void AtualizarResumo()
    {
        int total =
            Resultados.Count;


        int confirmadas =
            Resultados.Count(c =>
                c.Status == "Confirmada");


        int canceladas =
            Resultados.Count(c =>
                c.Status == "Cancelada");


        int porConfirmar =
            Resultados.Count(c =>
                c.Status == "Por confirmar");


        TotalConsultasLabel.Text =
            total.ToString();


        ConfirmadasLabel.Text =
            confirmadas.ToString();


        CanceladasLabel.Text =
            canceladas.ToString();


        PorConfirmarLabel.Text =
            porConfirmar.ToString();


        QuantidadeLabel.Text =
            $"{total} resultado(s)";
    }


    private async void GerarPdfButton_Clicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync(
            "Relatório PDF",
            "A geração do PDF será realizada pela API.",
            "OK");
    }


    private async void GerarExcelButton_Clicked(object sender, EventArgs e)
    {
        if (Resultados.Count == 0)
        {
            await DisplayAlertAsync(
                "Relatórios",
                "Não existem atendimentos para gerar o relatório.",
                "OK");

            return;
        }

        try
        {
            using var workbook = new XLWorkbook();

            var planilha = workbook.Worksheets.Add("Atendimentos");


            // TÍTULO
            planilha.Cell("A1").Value = "PediAgenda";
            planilha.Cell("A2").Value = "Relatório de Atendimentos";

            planilha.Range("A1:D1").Merge();
            planilha.Range("A2:D2").Merge();

            planilha.Cell("A1").Style.Font.Bold = true;
            planilha.Cell("A1").Style.Font.FontSize = 18;

            planilha.Cell("A2").Style.Font.Bold = true;
            planilha.Cell("A2").Style.Font.FontSize = 14;


            // FILTROS UTILIZADOS
            DateTime dataInicial =
                DataInicialPicker.Date ?? DateTime.Today;

            DateTime dataFinal =
                DataFinalPicker.Date ?? DateTime.Today;

            string pacienteSelecionado =
                PacientePicker.SelectedItem?.ToString() ?? "Todos";

            string statusSelecionado =
                StatusPicker.SelectedItem?.ToString() ?? "Todos";


            planilha.Cell("A4").Value = "Período:";
            planilha.Cell("B4").Value =
                $"{dataInicial:dd/MM/yyyy} até {dataFinal:dd/MM/yyyy}";

            planilha.Cell("A5").Value = "Paciente:";
            planilha.Cell("B5").Value = pacienteSelecionado;

            planilha.Cell("A6").Value = "Status:";
            planilha.Cell("B6").Value = statusSelecionado;


            // RESUMO

            planilha.Cell("A8").Value =
                "Resumo";

            planilha.Cell("A8").Style.Font.Bold =
                true;


            planilha.Cell("A9").Value =
                "Total de consultas";

            planilha.Cell("B9").Value =
                Resultados.Count;


            planilha.Cell("A10").Value =
                "Confirmadas";

            planilha.Cell("B10").Value =
                Resultados.Count(c =>
                    c.Status == "Confirmada");


            planilha.Cell("A11").Value =
                "Canceladas";

            planilha.Cell("B11").Value =
                Resultados.Count(c =>
                    c.Status == "Cancelada");


            planilha.Cell("A12").Value =
                "Por confirmar";

            planilha.Cell("B12").Value =
                Resultados.Count(c =>
                    c.Status == "Por confirmar");


            // CABEÇALHO DA TABELA
            int linhaCabecalho = 14;

            planilha.Cell(linhaCabecalho, 1).Value = "Paciente";
            planilha.Cell(linhaCabecalho, 2).Value = "Data";
            planilha.Cell(linhaCabecalho, 3).Value = "Horário";
            planilha.Cell(linhaCabecalho, 4).Value = "Status";

            var cabecalho =
                planilha.Range(
                    linhaCabecalho,
                    1,
                    linhaCabecalho,
                    4);

            cabecalho.Style.Font.Bold = true;


            // DADOS
            int linhaAtual = linhaCabecalho + 1;

            foreach (var consulta in Resultados)
            {
                planilha.Cell(linhaAtual, 1).Value =
                    consulta.Paciente;

                planilha.Cell(linhaAtual, 2).Value =
                    consulta.Data;

                planilha.Cell(linhaAtual, 2)
                    .Style.DateFormat.Format = "dd/MM/yyyy";

                planilha.Cell(linhaAtual, 3).Value =
                    consulta.Horario.ToString(@"hh\:mm");

                planilha.Cell(linhaAtual, 4).Value =
                    consulta.Status;

                linhaAtual++;
            }


            // AJUSTA O TAMANHO DAS COLUNAS
            planilha.Columns().AdjustToContents();


            // GERA O ARQUIVO EM MEMÓRIA
            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;


            // NOME DO ARQUIVO
            string nomeArquivo =
                $"Relatorio_PediAgenda_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";


            // PERMITE ESCOLHER ONDE SALVAR
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
                $"Não foi possível gerar o arquivo Excel.\n\n{ex.Message}",
                "OK");
        }
    }


    private async void VoltarButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}


// Modelo utilizado apenas pela tela de relatórios.
public class RelatorioConsultaItem
{
    public string Paciente { get; set; } = string.Empty;

    public DateTime Data { get; set; }

    public TimeSpan Horario { get; set; }

    public string Status { get; set; } = string.Empty;


    public string DataHoraFormatada =>
        $"{Data:dd/MM/yyyy} • {Horario:hh\\:mm}";
}