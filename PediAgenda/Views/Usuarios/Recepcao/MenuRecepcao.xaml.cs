using System.Collections.ObjectModel;
using PediAgenda.Dados;
using System.Net.Http.Headers;
using System.Text.Json;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class MenuRecepcao : ContentPage
{

    private static readonly HttpClient httpAgenda = new()
    {
        BaseAddress = new Uri("http://10.0.2.2:5000/"),
        Timeout = TimeSpan.FromSeconds(90)
    };


    public ObservableCollection<ConsultaRecepcaoResumo>
        ConsultasHoje
    {
        get;
    } =
        new();


    public MenuRecepcao()
    {
        InitializeComponent();


        BindingContext =
            this;
    }


    // =============================================
    // AO ABRIR / VOLTAR PARA O MENU
    // =============================================


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        AtualizarSaudacao();
        AtualizarData();

        ConsultasHoje.Clear();
        AtualizarQuantidadeConsultas();

        try
        {
            var consultas = await BuscarConsultasReaisAsync();

            foreach (var consulta in consultas)
            {
                ConsultasHoje.Add(consulta);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro ao carregar consultas",
                $"Não foi possível consultar a agenda: {ex.Message}",
                "OK");
        }
        finally
        {
            AtualizarQuantidadeConsultas();
        }
    }



    // =============================================
    // SAUDAÇÃO
    // =============================================

    private void AtualizarSaudacao()
    {
        string nome =
            SessaoUsuario.PrimeiroNome;


        SaudacaoLabel.Text =
            string.IsNullOrWhiteSpace(
                nome)

                ? "Olá!"

                : $"Olá, {nome}!";
    }


    // =============================================
    // DATA
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
    //
    // Consulta a agenda real através da API.
    // Exibe somente os horários que possuem
    // consultas não canceladas no banco de dados.


    private async Task<List<ConsultaRecepcaoResumo>> BuscarConsultasReaisAsync()
    {
        var agenda = await AgendaRecepcaoApi.ConsultarAsync(
            DateTime.Today);

        var consultas = new List<ConsultaRecepcaoResumo>();

        foreach (var medico in agenda.Medicos)
        {
            foreach (var horario in medico.Horarios)
            {
                var consulta = horario.Consulta;

                if (consulta == null ||
                    StatusConsulta.EhCancelada(consulta.Status))
                {
                    continue;
                }

                consultas.Add(new ConsultaRecepcaoResumo
                {
                    Horario = horario.Inicio,
                    Paciente = consulta.Paciente.Nome,
                    Medico = medico.Nome,
                    Status = consulta.Status
                });
            }
        }

        return consultas
            .OrderBy(c => c.Horario)
            .ThenBy(c => c.Medico)
            .ToList();
    }




    // =============================================
    // QUANTIDADE
    // =============================================

    private void AtualizarQuantidadeConsultas()
    {
        QuantidadeConsultasLabel.Text =
            ConsultasHoje.Count switch
            {
                0 =>
                    "0 consultas",

                1 =>
                    "1 consulta",

                _ =>
                    $"{ConsultasHoje.Count} consultas"
            };

        // O estado vazio so aparece apos a lista ser atualizada.
        bool semConsultas = ConsultasHoje.Count == 0;
        SemConsultasContainer.IsVisible = semConsultas;
        ConsultasCollectionView.IsVisible = !semConsultas;
    }


    // =============================================
    // ATUALIZAÇÃO MANUAL
    // =============================================


    private async void ConsultasRefreshView_Refreshing(
        object sender,
        EventArgs e)
    {
        try
        {
            var consultas = await BuscarConsultasReaisAsync();

            ConsultasHoje.Clear();

            foreach (var consulta in consultas)
            {
                ConsultasHoje.Add(consulta);
            }

            AtualizarQuantidadeConsultas();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro ao atualizar consultas",
                $"Não foi possível atualizar a agenda: {ex.Message}",
                "OK");
        }
        finally
        {
            ConsultasRefreshView.IsRefreshing = false;
        }
    }



    // =============================================
    // PACIENTES
    // =============================================

    private async void PacientesButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(
                PacientesRecepcao));
    }


    // =============================================
    // MÉDICOS
    // =============================================

    private async void MedicosButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(
                MedicosRecepcao));
    }
}