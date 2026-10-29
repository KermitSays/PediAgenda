using System.Collections.ObjectModel;
using PediAgenda.Dados;
using PediAgenda.Views.Usuarios.Medico;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class MedicosRecepcao : ContentPage
{
    // =============================================
    // HORÁRIOS EXIBIDOS
    // =============================================

    public ObservableCollection<HorarioAgendaRecepcao>
        HorariosFiltrados
    {
        get;
        set;
    } =
        new();


    // =============================================
    // CONTROLE DA DATA
    // =============================================

    private bool ajustandoData;


    // =============================================
    // MÉDICO SELECIONADO
    // =============================================
    //
    // Temporário enquanto os médicos ainda são
    // carregados localmente.
    //
    // Futuramente este valor será preenchido com
    // os dados retornados pela API.

    private string medicoSelecionado =
        string.Empty;

    private int? idMedicoSelecionado;

    // =============================================
    // CONSTRUTOR
    // =============================================

    public MedicosRecepcao()
    {
        InitializeComponent();


        BindingContext =
            this;


        // =========================================
        // MÉDICO INICIAL
        // =========================================

        medicoSelecionado = "Todos os médicos";

        MedicoSelecionadoLabel.Text = medicoSelecionado;


        // =========================================
        // PRIMEIRA DATA DISPONÍVEL
        // =========================================

        DateTime primeiraDataDisponivel =
    ProximoDiaUtil(
        DateTime.Today);


        DataPicker.MinimumDate =
            primeiraDataDisponivel;


        DataPicker.Date =
            primeiraDataDisponivel;


        // =========================================
        // CARREGA A AGENDA
        // =========================================

        CarregarAgenda();
    }


    // =============================================
    // AO VOLTAR PARA A TELA
    // =============================================

    protected override void OnAppearing()
    {
        base.OnAppearing();


        CarregarAgenda();
    }


    // =============================================
    // DIA ÚTIL
    // =============================================

    private bool EhDiaUtil(
        DateTime data)
    {
        return
            data.DayOfWeek !=
                DayOfWeek.Saturday

            &&

            data.DayOfWeek !=
                DayOfWeek.Sunday;
    }


    // =============================================
    // PRÓXIMO DIA ÚTIL
    // =============================================

    private DateTime ProximoDiaUtil(
        DateTime data)
    {
        DateTime resultado =
            data.Date;


        while (
            !EhDiaUtil(
                resultado))
        {
            resultado =
                resultado.AddDays(1);
        }


        return resultado;
    }


    // =============================================
    // SELECIONAR MÉDICO
    // =============================================


    private async void SelecionarMedicoButton_Clicked(
        object sender,
        EventArgs e)
    {
        var paginaSelecao = new SelecionarMedicoRecepcao();

        var medico = await paginaSelecao.AbrirAsync(this);

        if (medico == null)
            return;

        idMedicoSelecionado = medico.IdMedico;
        medicoSelecionado = medico.Nome;

        MedicoSelecionadoLabel.Text = medicoSelecionado;

        CarregarAgenda();
    }



    // =============================================
    // ALTERAÇÃO DA DATA
    // =============================================

    private async void DataPicker_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        if (ajustandoData)
            return;


        DateTime dataSelecionada =
            e.NewDate
            ?? DateTime.Today;


        // =========================================
        // BLOQUEIA FIM DE SEMANA
        // =========================================

        if (
            !EhDiaUtil(
                dataSelecionada))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "A clínica não possui atendimento aos sábados e domingos.",
                "OK");


            ajustandoData =
                true;


            DataPicker.Date =
                ProximoDiaUtil(
                    dataSelecionada);


            ajustandoData =
                false;
        }


        CarregarAgenda();
    }

    // Busca os horários reais da agenda do médico na API, filtrando por data e ID do médico, se fornecido.
    private async Task<List<HorarioAgendaRecepcao>> BuscarHorariosReaisAsync(
        DateTime data,
        int? idMedico = null)
    {
        var agenda = await AgendaRecepcaoApi.ConsultarAsync(
            data, idMedico);

        var horarios = new List<HorarioAgendaRecepcao>();

        foreach (var medico in agenda.Medicos)
        {
            foreach (var horario in medico.Horarios)
            {
                if (!TimeSpan.TryParse(
                    horario.Inicio, out TimeSpan horaInicio))
                {
                    continue;
                }

                string status = horario.Situacao switch
                {
                    "LIVRE" => "Disponível",
                    "OCUPADO" => "Agendado",
                    "BLOQUEADO" => "Bloqueado",
                    _ => "Indisponível"
                };

                horarios.Add(new HorarioAgendaRecepcao
                {
                    Id = horario.Id,
                    IdMedico = medico.Id,
                    Medico = medico.Nome,
                    Data = data.Date,
                    Horario = horaInicio,
                    Status = status,
                    IdConsulta = horario.Consulta?.Id,
                    StatusConsulta = horario.Consulta?.Status ?? "",
                    Paciente = horario.Consulta?.Paciente?.Nome ?? ""
                });
            }
        }

        return horarios
    .OrderBy(h => h.Medico)
    .ThenBy(h => h.Horario)
    .ToList();
    }



    // =============================================
    // CARREGA A AGENDA
    // =============================================


    private int versaoCarregamento = 0;

    private async void CarregarAgenda()
    {
        int versaoAtual = ++versaoCarregamento;

        HorariosFiltrados.Clear();
        QuantidadeHorariosLabel.Text = "Carregando...";

        MedicoSelecionadoLabel.Text =
        idMedicoSelecionado.HasValue
            ? medicoSelecionado
            : "Todos os médicos";

        DateTime data = DataPicker.Date ?? DateTime.Today;

        if (!EhDiaUtil(data))
        {
            QuantidadeHorariosLabel.Text = "0 horário(s)";
            return;
        }

        try
        {
            var horarios = await BuscarHorariosReaisAsync(
                data, idMedicoSelecionado);

            if (versaoAtual != versaoCarregamento)
                return;

            foreach (var horario in horarios)
            {
                HorariosFiltrados.Add(horario);
            }

            QuantidadeHorariosLabel.Text =
                $"{HorariosFiltrados.Count} horário(s)";
        }
        catch (Exception ex)
        {
            if (versaoAtual != versaoCarregamento)
                return;

            QuantidadeHorariosLabel.Text = "Erro ao carregar";

            await DisplayAlertAsync(
                "Erro na agenda",
                $"Não foi possível consultar a agenda: {ex.Message}",
                "OK");
        }
    }



    // =============================================
    // CARD DA AGENDA SELECIONADO
    // =============================================

    private async void AgendaCollectionView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (
            e.CurrentSelection.FirstOrDefault()
            is not HorarioAgendaRecepcao horario)
        {
            return;
        }


        // Permite tocar novamente no mesmo card.

        AgendaCollectionView.SelectedItem =
            null;


        // =========================================
        // HORÁRIO DISPONÍVEL
        // =========================================

        if (
            horario.Status ==
                "Disponível")
        {
            await AgendarHorarioAsync(
                horario);


            return;
        }


        // =========================================
        // CONSULTA AGENDADA
        // =========================================

        if (
            horario.Status ==
                "Agendado")
        {
            await AbrirConsultaAsync(
                horario);


            return;
        }


        // =========================================
        // HORÁRIO BLOQUEADO
        // =========================================

        if (
            horario.Status ==
                "Bloqueado")
        {
            await ExibirBloqueioAsync(
                horario);
        }
    }


    // =============================================
    // AGENDAR CONSULTA
    // =============================================

    private bool agendamentoRecepcaoEmAndamento;

    private async Task AgendarHorarioAsync(HorarioAgendaRecepcao horario)
    {
        if (agendamentoRecepcaoEmAndamento) return;

        var seletor = new SelecionarPacienteRecepcao();
        var paciente = await seletor.AbrirAsync(this);
        if (paciente is null) return;

        bool confirmar = await DisplayAlertAsync(
            "Confirmar agendamento",
            $"Paciente: {paciente.Nome}\nResponsável: {paciente.Responsavel}\n\n" +
            $"Médico: {horario.Medico}\nData: {horario.Data:dd/MM/yyyy}\n" +
            $"Horário: {horario.HorarioFormatado}",
            "AGENDAR", "CANCELAR");
        if (!confirmar) return;

        agendamentoRecepcaoEmAndamento = true;
        try
        {
            await AgendamentoRecepcaoApi.AgendarAsync(paciente.Id, horario.Id);
            await DisplayAlertAsync("Consulta agendada",
                "A consulta foi registrada.", "OK");
            CarregarAgenda();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Não foi possível agendar", ex.Message, "OK");
            CarregarAgenda();
        }
        finally
        {
            agendamentoRecepcaoEmAndamento = false;
        }
    }

    // =============================================
    // ABRIR CONSULTA
    // =============================================

    private async Task AbrirConsultaAsync(HorarioAgendaRecepcao horario)
    {
        if (horario.IdConsulta is not > 0)
        {
            await DisplayAlertAsync("Consulta",
                "Nao foi possivel identificar a consulta na API. Atualize a agenda.", "OK");
            CarregarAgenda();
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(DetalhesConsultaRecepcao),
            new Dictionary<string, object>
            {
                ["ConsultaId"] = horario.IdConsulta.Value.ToString()
            });
    }

    // =============================================
    // EXIBIR BLOQUEIO
    // =============================================

    private async Task ExibirBloqueioAsync(
        HorarioAgendaRecepcao horario)
    {
        BloqueioHorario?
            bloqueio =
                AgendaMedicaRecepcaoDados
                    .ObterBloqueioCompartilhado(
                        horario.Medico,
                        horario.Data,
                        horario.Horario);


        if (bloqueio == null)
        {
            await DisplayAlertAsync(
                "Horário bloqueado",
                "Este horário está indisponível.",
                "OK");


            return;
        }


        await DisplayAlertAsync(
            "Horário bloqueado",

            $"Data: {horario.Data:dd/MM/yyyy}\n" +
            $"Horário: {horario.HorarioFormatado}\n" +
            $"Motivo: {bloqueio.Motivo}\n\n" +
            "Use o botão “LIBERAR HORÁRIOS” para gerenciar este bloqueio.",

            "OK");
    }


    // =============================================
    // BLOQUEAR HORÁRIO — TELA ORIGINAL
    // =============================================

    private async void BloquearHorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (idMedicoSelecionado is not int idMedico || idMedico <= 0)
        {
            await DisplayAlertAsync(
                "Selecione um médico",
                "Selecione um médico antes de gerenciar a agenda.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(BloquearHorario),
            new Dictionary<string, object>
            {
                ["IdMedico"] = idMedico.ToString()
            });
    }

    // =============================================
    // LIBERAR HORÁRIOS — TELA ORIGINAL
    // =============================================

    private async void LiberarHorariosButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (idMedicoSelecionado is not int idMedico || idMedico <= 0)
        {
            await DisplayAlertAsync(
                "Selecione um médico",
                "Selecione um médico antes de gerenciar a agenda.",
                "OK");
            return;
        }

        await Shell.Current.GoToAsync(
            nameof(LiberarHorarios),
            new Dictionary<string, object>
            {
                ["IdMedico"] = idMedico.ToString()
            });
    }


    // =============================================
    // VOLTAR
    // =============================================

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            "..");
    }
}