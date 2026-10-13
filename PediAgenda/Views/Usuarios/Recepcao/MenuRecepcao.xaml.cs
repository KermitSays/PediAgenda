using System.Collections.ObjectModel;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class MenuRecepcao : ContentPage
{
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

    protected override void OnAppearing()
    {
        base.OnAppearing();


        AtualizarSaudacao();


        AtualizarData();


        AtualizarConsultasHoje();


        AtualizarQuantidadeConsultas();
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
    // O Menu usa a MESMA fonte compartilhada da
    // tela Médicos.
    //
    // Assim, se um horário estiver disponível na
    // agenda, ele não poderá continuar aparecendo
    // como consulta no Menu.

    private void AtualizarConsultasHoje()
    {
        ConsultasHoje.Clear();


        DateTime hoje =
            DateTime.Today;


        List<(
            TimeSpan Horario,
            ConsultaRecepcaoResumo Resumo)>
            consultas =
                new();


        // =========================================
        // PERCORRE TODOS OS MÉDICOS
        // =========================================

        foreach (
            string medico
            in AgendaMedicaRecepcaoDados
                .MedicosCompartilhados)
        {
            List<HorarioAgendaRecepcao>
                horarios =
                    AgendaMedicaRecepcaoDados
                        .ObterHorariosCompartilhados(
                            medico,
                            hoje);


            foreach (
                HorarioAgendaRecepcao horario
                in horarios)
            {
                // =================================
                // SOMENTE HORÁRIOS AGENDADOS
                // =================================
                //
                // Disponível:
                // não existe consulta ativa.
                //
                // Bloqueado:
                // não é uma consulta.
                //
                // Agendado:
                // existe uma consulta ativa.

                if (
                    horario.Status !=
                    "Agendado")
                {
                    continue;
                }


                // =================================
                // PROTEÇÃO EXTRA
                // =================================

                if (
                    StatusConsulta.EhCancelada(
                        horario.StatusConsulta))
                {
                    continue;
                }


                consultas.Add(
                    (
                        horario.Horario,

                        new ConsultaRecepcaoResumo
                        {
                            Horario =
                                horario
                                    .HorarioFormatado,

                            Paciente =
                                string.IsNullOrWhiteSpace(
                                    horario.Paciente)

                                    ? "Paciente"

                                    : horario.Paciente,

                            Medico =
                                medico,

                            Status =
                                string.IsNullOrWhiteSpace(
                                    horario.StatusConsulta)

                                    ? "Por confirmar"

                                    : StatusConsulta
                                        .Normalizar(
                                            horario.StatusConsulta)
                        }
                    ));
            }
        }


        // =============================================
        // ORDENA E REMOVE DUPLICAÇÕES
        // =============================================

        var consultasOrdenadas =
            consultas

                .GroupBy(item =>
                    new
                    {
                        item.Horario,

                        item.Resumo.Paciente,

                        item.Resumo.Medico
                    })

                .Select(grupo =>
                    grupo.First())

                .OrderBy(item =>
                    item.Horario)

                .ToList();


        foreach (
            var item
            in consultasOrdenadas)
        {
            ConsultasHoje.Add(
                item.Resumo);
        }
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
    }


    // =============================================
    // ATUALIZAÇÃO MANUAL
    // =============================================

    private void ConsultasRefreshView_Refreshing(
        object sender,
        EventArgs e)
    {
        AtualizarConsultasHoje();


        AtualizarQuantidadeConsultas();


        ConsultasRefreshView.IsRefreshing =
            false;
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


    // =============================================
    // ADICIONAR PACIENTE
    // =============================================

    private async void AdicionarPacienteButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(
                AdicionarPacienteRecepcao));
    }
}