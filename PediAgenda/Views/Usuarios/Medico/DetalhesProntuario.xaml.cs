using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class DetalhesProntuario : ContentPage, IQueryAttributable
{
    private int idConsulta;
    private CancellationTokenSource? carregamento;
    public DetalhesProntuario() { InitializeComponent(); }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        idConsulta = query.TryGetValue("IdConsulta", out var valor)
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

    private async void AtualizarButton_Clicked(object? sender, EventArgs e) => await CarregarAsync();

    private async Task CarregarAsync()
    {
        carregamento?.Cancel();
        using var atual = new CancellationTokenSource();
        carregamento = atual;
        long sessao = SessaoUsuario.Versao;
        TipoConsultaLabel.Text = DataConsultaLabel.Text = ObservacoesLabel.Text = "";
        EstadoLabel.Text = "Carregando registro…";
        try
        {
            if (idConsulta <= 0) throw new InvalidOperationException("Selecione um registro no histórico.");
            var registro = await MedicoApi.ObterProntuarioAsync(idConsulta, atual.Token);
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            TipoConsultaLabel.Text = registro.IdConsulta.ToString();
            DataConsultaLabel.Text = registro.DataRegistro.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            ObservacoesLabel.Text = registro.Observacoes;
            EstadoLabel.Text = "Registro do prontuário";
        }
        catch (Exception ex)
        {
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            EstadoLabel.Text = "Não foi possível carregar. Toque em Atualizar.";
            await DisplayAlertAsync("Prontuário", ex is InvalidOperationException
                ? ex.Message : "Falha ao carregar o registro. Tente novamente.", "OK");
        }
        finally
        {
            if (ReferenceEquals(carregamento, atual)) carregamento = null;
        }
    }

    private async void VoltarButton_Clicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}


