using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class ProntuarioPaciente : ContentPage, IQueryAttributable
{
    private int idPaciente;
    private CancellationTokenSource? carregamento;
    private bool navegando;

    public ProntuarioPaciente() { InitializeComponent(); }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        idPaciente = query.TryGetValue("IdPaciente", out var valor)
            && int.TryParse(valor?.ToString(), out int id) ? id : 0;
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


    private async Task CarregarAsync()
    {
        carregamento?.Cancel();
        using var atual = new CancellationTokenSource();
        carregamento = atual;
        long sessao = SessaoUsuario.Versao;
        NomePacienteLabel.Text = DataNascimentoLabel.Text = ResponsavelLabel.Text = "";
        HistoricoCollectionView.ItemsSource = null;
        HistoricoCollectionView.IsVisible = false;
        SemHistoricoLabel.IsVisible = false;
        EstadoLabel.Text = "Carregando prontuário…";
        try
        {
            if (idPaciente <= 0) throw new InvalidOperationException("Selecione um paciente na lista.");
            var pacientes = await MedicoApi.ObterPacientesAsync(atual.Token);
            var paciente = pacientes.FirstOrDefault(p => p.Id == idPaciente)
                ?? throw new InvalidOperationException("Paciente não encontrado na sua agenda.");
            var historico = await MedicoApi.ObterHistoricoAsync(idPaciente, atual.Token);
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            NomePacienteLabel.Text = paciente.Nome;
            DataNascimentoLabel.Text = paciente.DataNascimento.ToString("dd/MM/yyyy");
            ResponsavelLabel.Text = paciente.Responsavel;
            HistoricoCollectionView.ItemsSource = historico.OrderByDescending(p => p.DataRegistro).ThenByDescending(p => p.Id).ToList();
            HistoricoCollectionView.IsVisible = historico.Count > 0;
            SemHistoricoLabel.IsVisible = historico.Count == 0;
            EstadoLabel.Text = "Registros do prontuário";
        }
        catch (Exception ex)
        {
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            EstadoLabel.Text = "Não foi possível carregar. Volte à tela anterior e tente novamente.";
            await DisplayAlertAsync("Prontuário", ex is InvalidOperationException
                ? ex.Message : "Falha ao carregar o prontuário. Tente novamente.", "OK");
        }
        finally
        {
            if (ReferenceEquals(carregamento, atual)) carregamento = null;
        }
    }

    private async void ConsultaHistorico_Clicked(object? sender, TappedEventArgs e)
    {
        if (navegando || sender is not Border { BindingContext: ProntuarioMedicoResposta registro }) return;
        navegando = true;
        try
        {
            await Shell.Current.GoToAsync(nameof(DetalhesProntuario),
                new Dictionary<string, object> { ["IdConsulta"] = registro.IdConsulta });
        }
        finally { navegando = false; }
    }

    private async void VoltarButton_Clicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}


