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
        string nomeCompleto = SessaoUsuario.Nome?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nomeCompleto))
        {
            SaudacaoLabel.Text = "Olá!";
            return;
        }

        string[] partes = nomeCompleto.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        string primeiroTermo = partes[0].TrimEnd('.');

        string titulo = primeiroTermo.ToLowerInvariant() switch
        {
            "dr" => "Dr.",
            "dra" => "Dra.",
            _ => string.Empty
        };

        string primeiroNome = !string.IsNullOrEmpty(titulo)
            ? (partes.Length > 1 ? partes[1] : string.Empty)
            : partes[0];

        SaudacaoLabel.Text = string.IsNullOrWhiteSpace(primeiroNome)
            ? "Olá!"
            : string.IsNullOrEmpty(titulo)
                ? $"Olá, {primeiroNome}!"
                : $"Olá, {titulo} {primeiroNome}!";
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

    private async void ConsultasRefreshView_Refreshing(object? sender, EventArgs e)
    {
        try
        {
            await AtualizarConsultasAsync();
        }
        finally
        {
            ConsultasRefreshView.IsRefreshing = false;
        }
    }

    private async Task AtualizarConsultasAsync()
    {
        carregamento?.Cancel();
        using var atual = new CancellationTokenSource();
        carregamento = atual;
        long sessao = SessaoUsuario.Versao;
        ConsultasLista.Clear();
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
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
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
        }
        catch (OperationCanceledException) when (atual.IsCancellationRequested) { }
        catch (Exception) when (atual.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (sessao != SessaoUsuario.Versao) return;
            EstadoAgendaLabel.Text = "Não foi possível carregar as consultas. Puxe a lista para baixo para tentar novamente.";
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






    }

 
    // =============================================
    // PACIENTES
    // =============================================

    private async void MedicoPacientesButton_Clicked(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(PacientesMedico));
    }


    // =============================================
    // CONSULTAS
    // =============================================

    private async void MedicoConsultasButton_Clicked(
        object? sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(ConsultasMedico));
    }


    // =============================================
    // RELATÓRIOS
    // =============================================

    private async void MedicoRelatoriosButton_Clicked(
        object? sender,
        TappedEventArgs e)
    {
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


