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

        medicoSelecionado =
            AgendaMedicaRecepcaoDados
                .MedicosCompartilhados
                .FirstOrDefault()
            ?? string.Empty;


        MedicoSelecionadoLabel.Text =
            medicoSelecionado;


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
        SelecionarMedicoRecepcao
            paginaSelecao =
                new();


        SelecionarMedicoRecepcao
            .MedicoSelecaoItem?
            medico =
                await paginaSelecao
                    .AbrirAsync(
                        this);


        if (medico == null)
            return;


        medicoSelecionado =
            medico.Nome;


        MedicoSelecionadoLabel.Text =
            medicoSelecionado;


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


    // =============================================
    // CARREGA A AGENDA
    // =============================================

    private void CarregarAgenda()
    {
        HorariosFiltrados.Clear();


        string medico =
            medicoSelecionado;


        if (
            string.IsNullOrWhiteSpace(
                medico))
        {
            QuantidadeHorariosLabel.Text =
                "0 horário(s)";


            return;
        }


        DateTime data =
            DataPicker.Date
            ?? ProximoDiaUtil(
                DateTime.Today);


        if (
            !EhDiaUtil(
                data))
        {
            QuantidadeHorariosLabel.Text =
                "0 horário(s)";


            return;
        }


        // =========================================
        // FONTE COMPARTILHADA
        // =========================================

        List<HorarioAgendaRecepcao>
            horarios =
                AgendaMedicaRecepcaoDados
                    .ObterHorariosCompartilhados(
                        medico,
                        data);


        foreach (
            HorarioAgendaRecepcao horario
            in horarios)
        {
            HorariosFiltrados.Add(
                horario);
        }


        QuantidadeHorariosLabel.Text =
            $"{HorariosFiltrados.Count} horário(s)";
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

    private async Task AgendarHorarioAsync(
        HorarioAgendaRecepcao horario)
    {
        if (
            PacientesRecepcaoDados
                .Pacientes.Count ==
            0)
        {
            await DisplayAlertAsync(
                "Nenhum paciente",
                "Não existem pacientes disponíveis para realizar o agendamento.",
                "OK");


            return;
        }


        // =========================================
        // SELETOR PESQUISÁVEL DE PACIENTES
        // =========================================

        SelecionarPacienteRecepcao
            paginaSelecao =
                new();


        PacienteRecepcaoItem?
            paciente =
                await paginaSelecao
                    .AbrirAsync(
                        this);


        if (paciente == null)
            return;


        // =========================================
        // CONFIRMAÇÃO
        // =========================================

        bool confirmar =
            await DisplayAlertAsync(
                "Confirmar agendamento",

                $"Paciente: {paciente.Nome}\n" +
                $"Responsável: {paciente.Responsavel}\n\n" +
                $"Médico: {horario.Medico}\n" +
                $"Data: {horario.Data:dd/MM/yyyy}\n" +
                $"Horário: {horario.HorarioFormatado}",

                "AGENDAR",
                "CANCELAR");


        if (!confirmar)
            return;


        // =========================================
        // RESERVA O HORÁRIO COMPARTILHADO
        // =========================================

        bool reservado =
            HorarioAgendamentoDados
                .ReservarHorario(
                    horario.Id);


        if (!reservado)
        {
            await DisplayAlertAsync(
                "Horário indisponível",
                "Este horário não está mais disponível.",
                "OK");


            CarregarAgenda();


            return;
        }


        // =========================================
        // CRIA CONSULTA LOCAL
        // =========================================
        //
        // Futuramente esta operação será feita
        // através da API.

        paciente.Consultas.Add(
            new ConsultaPacienteRecepcaoItem
            {
                HorarioId =
                    horario.Id,

                Data =
                    horario.Data,

                Horario =
                    horario.Horario,

                Medico =
                    horario.Medico,

                Especialidade =
                    "Pediatria Geral",

                TipoAtendimento =
                    "Particular",

                Status =
                    StatusConsulta.PorConfirmar
            });


        CarregarAgenda();


        await DisplayAlertAsync(
            "Consulta agendada",
            $"A consulta de {paciente.Nome} foi agendada com sucesso.",
            "OK");
    }


    // =============================================
    // ABRIR CONSULTA
    // =============================================

    private async Task AbrirConsultaAsync(
        HorarioAgendaRecepcao horario)
    {
        // =========================================
        // PROCURA CONSULTA DA RECEPÇÃO
        // =========================================

        var consultaRecepcao =
            PacientesRecepcaoDados
                .Pacientes
                .SelectMany(
                    paciente =>
                        paciente.Consultas.Select(
                            consulta =>
                                new
                                {
                                    Paciente =
                                        paciente,

                                    Consulta =
                                        consulta
                                }))
                .FirstOrDefault(item =>
                    item.Consulta.HorarioId ==
                        horario.Id

                    &&

                    !StatusConsulta.EhCancelada(
                        item.Consulta.Status));


        // =========================================
        // CONSULTA GERENCIADA PELA RECEPÇÃO
        // =========================================

        if (
            consultaRecepcao !=
            null)
        {
            await Shell.Current.GoToAsync(
                nameof(
                    DetalhesConsultaRecepcao),

                new Dictionary<string, object>
                {
                    {
                        "HorarioId",
                        horario.Id.ToString()
                    }
                });


            return;
        }


        // =========================================
        // CONSULTA LOCAL DO MÉDICO
        // =========================================
        //
        // ConsultasMedicoDados representa,
        // temporariamente, o médico de ID 1.
        //
        // Quando a API estiver integrada,
        // Médico e Recepção consultarão a mesma
        // entidade de consulta.

        int idMedico =
            AgendaMedicaRecepcaoDados
                .ObterIdMedico(
                    horario.Medico);


        ConsultaMedico?
            consultaMedico =
                null;


        if (
            idMedico ==
            1)
        {
            consultaMedico =
                ConsultasMedicoDados
                    .Consultas
                    .FirstOrDefault(c =>
                        c.Data.Date ==
                            horario.Data.Date

                        &&

                        c.Horario ==
                            horario.Horario

                        &&

                        !StatusConsulta.EhCancelada(
                            c.Status));
        }


        // =========================================
        // OCUPADO SEM DADOS LOCAIS
        // =========================================

        if (
            consultaMedico ==
            null)
        {
            await DisplayAlertAsync(
                "Horário ocupado",

                string.IsNullOrWhiteSpace(
                    horario.Paciente)

                    ? "Este horário já está ocupado."
                    : $"Paciente: {horario.Paciente}",

                "OK");


            return;
        }


        // =========================================
        // RESUMO ADMINISTRATIVO
        // =========================================
        //
        // A Recepção vê somente informações
        // necessárias para administrar a consulta.
        //
        // Não são exibidos prontuário, diagnóstico,
        // observações clínicas ou outros dados
        // médicos.

        string detalhes =
            $"Paciente: {consultaMedico.Paciente}\n" +
            $"Médico: {horario.Medico}\n" +
            $"Data: {horario.Data:dd/MM/yyyy}\n" +
            $"Horário: {horario.HorarioFormatado}\n" +
            $"Status: {consultaMedico.Status}";


        // =========================================
        // CONSULTA REALIZADA
        // =========================================

        if (
            StatusConsulta.EhRealizada(
                consultaMedico.Status))
        {
            await DisplayAlertAsync(
                "Consulta",
                detalhes,
                "OK");


            return;
        }


        // =========================================
        // CANCELAR CONSULTA
        // =========================================

        bool cancelar =
            await DisplayAlertAsync(
                "Consulta",

                detalhes +
                "\n\nDeseja cancelar esta consulta e liberar o horário?",

                "CANCELAR CONSULTA",
                "VOLTAR");


        if (!cancelar)
            return;


        bool confirmar =
            await DisplayAlertAsync(
                "Confirmar cancelamento",

                $"Deseja realmente cancelar a consulta de " +
                $"{consultaMedico.Paciente}?",

                "SIM",
                "NÃO");


        if (!confirmar)
            return;


        consultaMedico.Status =
            StatusConsulta.Cancelada;


        HorarioAgendamentoDados
            .LiberarHorario(
                horario.Id);


        CarregarAgenda();


        await DisplayAlertAsync(
            "Consulta cancelada",
            "A consulta foi cancelada e o horário foi liberado.",
            "OK");
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
    // BLOQUEAR HORÁRIO
    // =============================================

    private async void BloquearHorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        string medico =
            medicoSelecionado;


        int idMedico =
            AgendaMedicaRecepcaoDados
                .ObterIdMedico(
                    medico);


        if (idMedico <= 0)
        {
            await DisplayAlertAsync(
                "Selecione um médico",
                "Selecione o médico cuja agenda deseja bloquear.",
                "OK");


            return;
        }


        await Shell.Current.GoToAsync(
            nameof(
                BloquearHorario),

            new Dictionary<string, object>
            {
                {
                    "IdMedico",
                    idMedico.ToString()
                }
            });
    }


    // =============================================
    // LIBERAR HORÁRIOS
    // =============================================

    private async void LiberarHorariosButton_Clicked(
        object sender,
        EventArgs e)
    {
        string medico =
            medicoSelecionado;


        int idMedico =
            AgendaMedicaRecepcaoDados
                .ObterIdMedico(
                    medico);


        if (idMedico <= 0)
        {
            await DisplayAlertAsync(
                "Selecione um médico",
                "Selecione o médico cuja agenda deseja gerenciar.",
                "OK");


            return;
        }


        await Shell.Current.GoToAsync(
            nameof(
                LiberarHorarios),

            new Dictionary<string, object>
            {
                {
                    "IdMedico",
                    idMedico.ToString()
                }
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