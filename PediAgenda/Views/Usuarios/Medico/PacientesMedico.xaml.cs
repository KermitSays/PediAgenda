using System.Collections.ObjectModel;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class PacientesMedico : ContentPage
{
    private List<PacienteMedicoResposta> pacientes = new();
    public ObservableCollection<PacienteMedicoResposta> PacientesFiltrados { get; } = new();
    private CancellationTokenSource? carregamento;
    private bool navegando;

    public PacientesMedico()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    protected override void OnDisappearing()
    {
        carregamento?.Cancel();
        base.OnDisappearing();
    }

    private async void AtualizarButton_Clicked(object? sender, EventArgs e) => await CarregarAsync();

    private async Task CarregarAsync()
    {
        carregamento?.Cancel();
        using var atual = new CancellationTokenSource();
        carregamento = atual;
        long sessao = SessaoUsuario.Versao;
        pacientes.Clear();
        PacientesFiltrados.Clear();
        PacientesCollectionView.IsVisible = false;
        EstadoLabel.Text = "Carregando pacientes…";
        try
        {
            var resposta = await MedicoApi.ObterPacientesAsync(atual.Token);
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            pacientes = resposta;
            Filtrar();
            EstadoLabel.Text = pacientes.Count == 0
                ? "Nenhum paciente vinculado à sua agenda." : "Pacientes vinculados à sua agenda";
            PacientesCollectionView.IsVisible = pacientes.Count > 0;
        }
        catch (Exception ex)
        {
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            EstadoLabel.Text = "Não foi possível carregar. Toque em Atualizar.";
            await DisplayAlertAsync("Pacientes", ex is InvalidOperationException
                ? ex.Message : "Falha ao carregar os pacientes. Tente novamente.", "OK");
        }
        finally
        {
            if (ReferenceEquals(carregamento, atual)) carregamento = null;
        }
    }

    private void BuscarPacienteEntry_TextChanged(object? sender, TextChangedEventArgs e) => Filtrar();

    private void Filtrar()
    {
        string busca = BuscarPacienteEntry?.Text?.Trim() ?? "";
        PacientesFiltrados.Clear();
        foreach (var paciente in pacientes.Where(p => p.Nome.Contains(busca,
            StringComparison.OrdinalIgnoreCase)).OrderBy(p => p.Nome))
            PacientesFiltrados.Add(paciente);
    }

    private async void PacientesMedico_Clicked(object? sender, TappedEventArgs e)
    {
        if (navegando || sender is not Border { BindingContext: PacienteMedicoResposta paciente }) return;
        navegando = true;
        try
        {
            await Shell.Current.GoToAsync(nameof(ProntuarioPaciente),
                new Dictionary<string, object> { ["IdPaciente"] = paciente.Id });
        }
        finally { navegando = false; }
    }
}


