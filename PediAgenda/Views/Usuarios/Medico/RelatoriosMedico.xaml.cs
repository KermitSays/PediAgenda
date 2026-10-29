using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Storage;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class RelatoriosMedico : ContentPage
{
    public ObservableCollection<LinhaRelatorioMedicoApi> Resultados { get; } = new();
    private CancellationTokenSource? carregamento;
    private RelatorioMedicoDocumento? documento;
    private long sessaoDocumento;
    private bool exportando;

    public RelatoriosMedico()
    {
        InitializeComponent();
        BindingContext = this;
        DataInicialPicker.Date = DateTime.Today.AddMonths(-1);
        DataFinalPicker.Date = DateTime.Today;
        DataInicialPicker.DateSelected += PeriodoAlterado;
        DataFinalPicker.DateSelected += PeriodoAlterado;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await BuscarAsync();
    }

    protected override void OnDisappearing()
    {
        carregamento?.Cancel();
        base.OnDisappearing();
    }

    private void LimparResultado()
    {
        documento = null;
        Resultados.Clear();
        TotalRealizadasLabel.Text = "0";
        ResultadosCollectionView.IsVisible = false;
        GerarExcelButton.IsEnabled = false;
    }

    private void PeriodoAlterado(object? sender, DateChangedEventArgs e)
    {
        carregamento?.Cancel();
        LimparResultado();
        EstadoLabel.Text = "Período alterado. Toque em Buscar atendimentos.";
    }

    private async void BuscarButton_Clicked(object? sender, EventArgs e) => await BuscarAsync();

    private async Task BuscarAsync()
    {
        carregamento?.Cancel();
        using var atual = new CancellationTokenSource();
        carregamento = atual;
        long sessao = SessaoUsuario.Versao;
        LimparResultado();
        EstadoLabel.Text = "Carregando atendimentos…";
        try
        {
            var inicio = DateOnly.FromDateTime(DataInicialPicker.Date ?? DateTime.Today);
            var fim = DateOnly.FromDateTime(DataFinalPicker.Date ?? DateTime.Today);
            if (fim < inicio || fim.DayNumber - inicio.DayNumber + 1 > 366)
                throw new InvalidOperationException("Selecione um período válido de até 366 dias.");
            var resposta = await MedicoApi.ObterRelatorioAsync(inicio, fim, atual.Token);
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            documento = RelatorioMedicoDocumento.Criar(resposta, inicio, fim);
            sessaoDocumento = sessao;
            foreach (var item in documento.Atendimentos) Resultados.Add(item);
            TotalRealizadasLabel.Text = Resultados.Count.ToString();
            EstadoLabel.Text = $"Período consultado: {inicio:dd/MM/yyyy} até {fim:dd/MM/yyyy}";
            ResultadosCollectionView.IsVisible = true;
            GerarExcelButton.IsEnabled = Resultados.Count > 0 && !exportando;
        }
        catch (Exception ex)
        {
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            EstadoLabel.Text = "Não foi possível carregar. Toque em Buscar atendimentos para tentar novamente.";
            await DisplayAlertAsync("Relatórios", ex is InvalidOperationException
                ? ex.Message : "Falha ao carregar os atendimentos. Tente novamente.", "OK");
        }
        finally
        {
            if (ReferenceEquals(carregamento, atual)) carregamento = null;
        }
    }

    private async void GerarExcelButton_Clicked(object? sender, EventArgs e)
    {
        if (exportando || documento is not { } resultado || resultado.Atendimentos.Count == 0) return;
        if (sessaoDocumento != SessaoUsuario.Versao)
        {
            LimparResultado();
            EstadoLabel.Text = "Sua sessão mudou. Consulte os atendimentos novamente.";
            return;
        }
        exportando = true;
        GerarExcelButton.IsEnabled = false;
        try
        {
            using var stream = ExportadorExcelMedico.Gerar(resultado);
            var salvo = await FileSaver.Default.SaveAsync($"Relatorio_PediAgenda_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx", stream);
            if (sessaoDocumento != SessaoUsuario.Versao) return;
            await DisplayAlertAsync("Relatórios", salvo.IsSuccessful
                ? "O arquivo Excel foi salvo com sucesso." : "O arquivo não foi salvo.", "OK");
        }
        catch (Exception)
        {
            await DisplayAlertAsync("Relatórios", "Não foi possível salvar o Excel. Tente novamente.", "OK");
        }
        finally
        {
            exportando = false;
            GerarExcelButton.IsEnabled = documento?.Atendimentos.Count > 0
                && sessaoDocumento == SessaoUsuario.Versao;
        }
    }

    private async void VoltarButton_Clicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
