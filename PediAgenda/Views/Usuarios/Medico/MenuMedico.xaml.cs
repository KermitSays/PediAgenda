using System.Collections.ObjectModel;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class MenuMedico : ContentPage
{
    public ObservableCollection<ConsultaHoje>
        ConsultasLista
    {
        get;
        set;
    } = new();


    public MenuMedico()
    {
        InitializeComponent();


        BindingContext =
            this;
    }


    protected override async void OnAppearing()
    {
        base.OnAppearing();


        AtualizarSaudacao();


        AtualizarData();


        await AtualizarConsultasAsync();
    }


    // =============================================
    // SAUDAÇÃO
    // =============================================

    private void AtualizarSaudacao()
    {
        string nome =
            SessaoUsuario.PrimeiroNome;


        SaudacaoLabel.Text =
            string.IsNullOrWhiteSpace(nome)

                ? "Olá, Dr(a).!"

                : $"Olá, Dr(a). {nome}!";
    }


    // =============================================
    // DATA ATUAL
    // =============================================

    private void AtualizarData()
    {
        DataAtualLabel.Text =
            $"Hoje é " +
            $"{DateTime.Today:dd 'de' MMMM 'de' yyyy}";
    }


    // =============================================
    // CONSULTAS DE HOJE
    // =============================================

    private CancellationTokenSource? carregamento;

    protected override void OnDisappearing()
    {
        carregamento?.Cancel();
        base.OnDisappearing();
    }

    private async void AtualizarButton_Clicked(object sender, EventArgs e)
        => await AtualizarConsultasAsync();

    private async Task AtualizarConsultasAsync()
    {
        carregamento?.Cancel();
        using var atual = new CancellationTokenSource();
        carregamento = atual;
        int usuario = SessaoUsuario.IdUsuario;
        ConsultasLista.Clear();
        ConsultasCollectionView.IsVisible = false;
        SemConsultasContainer.IsVisible = false;
        EstadoAgendaLabel.Text = MedicoApi.ModoTeste
            ? "Teste visual — dados de exemplo" : "Carregando consultas…";
        try
        {
            if (MedicoApi.ModoTeste)
            {
                CarregarConsultasHoje();
                return;
            }
            var horarios = await MedicoApi.ObterAgendaAsync(DateTime.Today, atual.Token);
            if (atual.IsCancellationRequested || usuario != SessaoUsuario.IdUsuario) return;
            foreach (var horario in horarios)
            {
                var consulta = horario.Consulta;
                if (consulta == null || StatusConsulta.EhCancelada(consulta.Status)) continue;
                ConsultasLista.Add(new ConsultaHoje
                {
                    Horario = horario.Inicio,
                    Paciente = consulta.Paciente.Nome,
                    Status = StatusConsulta.Normalizar(consulta.Status)
                });
            }
            EstadoAgendaLabel.Text = "Consultas do médico conectado";
            ConsultasCollectionView.IsVisible = ConsultasLista.Count > 0;
            SemConsultasContainer.IsVisible = ConsultasLista.Count == 0;
        }
        catch (OperationCanceledException) when (atual.IsCancellationRequested) { }
        catch (Exception) when (atual.IsCancellationRequested) { }
        catch (Exception ex)
        {
            EstadoAgendaLabel.Text = "Não foi possível carregar as consultas. Toque em Atualizar.";
            await DisplayAlertAsync("Consultas", ex is InvalidOperationException
                ? ex.Message : "Não foi possível carregar a agenda. Tente novamente.", "OK");
        }
        finally
        {
            if (ReferenceEquals(carregamento, atual)) carregamento = null;
        }
    }

    private void CarregarConsultasHoje()
    {
        ConsultasLista.Clear();


        List<ConsultaMedico>
            consultasHoje =
                ConsultasMedicoDados
                    .ObterConsultasAtivasPorData(
                        DateTime.Today);


        foreach (
            ConsultaMedico consulta
            in consultasHoje)
        {
            ConsultasLista.Add(
                new ConsultaHoje
                {
                    IdConsulta =
                        consulta.Id,

                    Horario =
                        consulta.HorarioTexto,

                    Paciente =
                        consulta.Paciente,

                    Status =
                        consulta.Status
                });
        }


        bool temConsultas =
            ConsultasLista.Count > 0;


        ConsultasCollectionView.IsVisible =
            temConsultas;


        SemConsultasContainer.IsVisible =
            !temConsultas;
    }

 
    // =============================================
    // PACIENTES
    // =============================================

    private async void MedicoPacientesButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        if (!MedicoApi.ModoTeste)
        {
            await DisplayAlertAsync("Integração em andamento",
                "Esta tela ainda usa dados de exemplo. Ela permanece disponível pelo acesso de teste visual.", "OK");
            return;
        }
        await Shell.Current.GoToAsync(
            nameof(PacientesMedico));
    }


    // =============================================
    // CONSULTAS
    // =============================================

    private async void MedicoConsultasButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(ConsultasMedico));
    }


    // =============================================
    // RELATÓRIOS
    // =============================================

    private async void MedicoRelatoriosButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        if (!MedicoApi.ModoTeste)
        {
            await DisplayAlertAsync("Integração em andamento",
                "Esta tela ainda usa dados de exemplo. Ela permanece disponível pelo acesso de teste visual.", "OK");
            return;
        }
        await Shell.Current.GoToAsync(
            nameof(RelatoriosMedico));
    }
}


// =============================================
// MODELO VISUAL
// =============================================

public class ConsultaHoje
{
    public Guid IdConsulta
    {
        get;
        set;
    }


    public string Horario
    {
        get;
        set;
    } = string.Empty;


    public string Paciente
    {
        get;
        set;
    } = string.Empty;


    public string Status
    {
        get;
        set;
    } = string.Empty;
}
