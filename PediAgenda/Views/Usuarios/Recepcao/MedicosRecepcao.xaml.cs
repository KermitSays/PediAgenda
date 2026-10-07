using System.Collections.ObjectModel;
using PediAgenda.Views.Usuarios.Medico;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class MedicosRecepcao : ContentPage
{
    public ObservableCollection<HorarioAgendaRecepcao>
        HorariosFiltrados
    {
        get;
        set;
    } = new();


    private bool ajustandoData;


    public MedicosRecepcao()
    {
        InitializeComponent();


        BindingContext =
            this;


        CarregarMedicos();


        DateTime primeiraDataDisponivel =
            ProximoDiaUtil(
                DateTime.Today);


        DataPicker.MinimumDate =
            primeiraDataDisponivel;


        DataPicker.Date =
            primeiraDataDisponivel;


        CarregarAgenda();
    }


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


        while (!EhDiaUtil(
            resultado))
        {
            resultado =
                resultado.AddDays(1);
        }


        return resultado;
    }


    // =============================================
    // MÉDICOS
    // =============================================

    private void CarregarMedicos()
    {
        MedicoPicker.Items.Clear();


        foreach (
            string medico
            in AgendaMedicaRecepcaoDados
                .MedicosCompartilhados)
        {
            MedicoPicker.Items.Add(
                medico);
        }


        if (
            MedicoPicker.Items.Count >
            0)
        {
            MedicoPicker.SelectedIndex =
                0;
        }
    }


    // =============================================
    // ALTERAÇÃO DO MÉDICO
    // =============================================

    private void Filtro_Changed(
        object sender,
        EventArgs e)
    {
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


        if (!EhDiaUtil(
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
        string medicoSelecionado =
            MedicoPicker.SelectedItem?
                .ToString()
            ?? string.Empty;


        DateTime dataSelecionada =
            DataPicker.Date
            ?? ProximoDiaUtil(
                DateTime.Today);


        HorariosFiltrados.Clear();


        if (!EhDiaUtil(
            dataSelecionada))
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
                        medicoSelecionado,
                        dataSelecionada);


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
    // HORÁRIO
    // =============================================

    private async void HorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            sender is not Button botao

            ||

            botao.CommandParameter
                is not HorarioAgendaRecepcao horario)
        {
            return;
        }


        if (!EhDiaUtil(
            horario.Data))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "Não é possível utilizar horários aos sábados ou domingos.",
                "OK");


            return;
        }


        // =========================================
        // DISPONÍVEL
        // =========================================

        if (
            horario.Status ==
                "Disponível")
        {
            await DisplayAlertAsync(
                "Agendar consulta",

                $"Horário selecionado:\n" +
                $"{horario.HorarioFormatado}\n\n" +

                $"Médico: {horario.Medico}\n" +
                $"Data: {horario.Data:dd/MM/yyyy}\n\n" +

                "O agendamento da Recepção será conectado " +
                "à fonte compartilhada na próxima etapa.",

                "OK");


            return;
        }


        // =========================================
        // AGENDADO
        // =========================================

        if (
            horario.Status ==
                "Agendado")
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
        // BLOQUEADO
        // =========================================

        if (
            horario.Status ==
                "Bloqueado")
        {
            BloqueioHorario? bloqueio =
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


            string periodo =
                bloqueio.DataInicial.Date ==
                bloqueio.DataFinal.Date

                    ? $"{bloqueio.DataInicial:dd/MM/yyyy} • " +
                      $"{bloqueio.HorarioInicial:hh\\:mm} às " +
                      $"{bloqueio.HorarioFinal:hh\\:mm}"

                    : $"{bloqueio.DataInicial:dd/MM/yyyy} até " +
                      $"{bloqueio.DataFinal:dd/MM/yyyy} • " +
                      $"{bloqueio.HorarioInicial:hh\\:mm} às " +
                      $"{bloqueio.HorarioFinal:hh\\:mm}";


            bool confirmar =
                await DisplayAlertAsync(
                    "Liberar bloqueio",

                    $"Este horário faz parte do bloqueio:\n\n" +
                    $"{periodo}\n\n" +
                    $"Motivo: {bloqueio.Motivo}\n\n" +
                    "Deseja liberar todo este período?",

                    "LIBERAR",
                    "CANCELAR");


            if (!confirmar)
                return;


            // LIBERA A MESMA FONTE USADA PELO MÉDICO

            HorarioAgendamentoDados
                .LiberarBloqueio(
                    bloqueio.Id);


            BloqueiosMedico.Bloqueios
                .Remove(
                    bloqueio);


            CarregarAgenda();


            await DisplayAlertAsync(
                "Período liberado",
                "O bloqueio foi liberado com sucesso.",
                "OK");
        }
    }
}