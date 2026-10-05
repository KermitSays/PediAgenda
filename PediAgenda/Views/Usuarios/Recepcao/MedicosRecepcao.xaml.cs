using System.Collections.ObjectModel;

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
                DayOfWeek.Saturday &&

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


        while (!EhDiaUtil(resultado))
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
            in AgendaMedicaRecepcaoDados.Medicos)
        {
            MedicoPicker.Items.Add(
                medico);
        }


        if (MedicoPicker.Items.Count > 0)
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


        // FINAL DE SEMANA NÃO TEM AGENDA

        if (!EhDiaUtil(
            dataSelecionada))
        {
            QuantidadeHorariosLabel.Text =
                "0 horário(s)";


            return;
        }


        var horarios =
            AgendaMedicaRecepcaoDados.Horarios
                .Where(h =>
                    h.Medico ==
                        medicoSelecionado &&

                    h.Data.Date ==
                        dataSelecionada.Date)
                .OrderBy(h =>
                    h.Horario)
                .ToList();


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
            sender is not Button botao ||

            botao.CommandParameter
            is not HorarioAgendaRecepcao horario)
        {
            return;
        }


        // SEGURANÇA EXTRA:
        // nunca permite ação em final de semana.

        if (!EhDiaUtil(
            horario.Data))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "Não é possível utilizar horários aos sábados ou domingos.",
                "OK");


            return;
        }


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

                "O fluxo de agendamento será conectado em seguida.",

                "OK");


            return;
        }


        if (
            horario.Status ==
            "Agendado")
        {
            await Shell.Current.GoToAsync(
                $"{nameof(DetalhesConsultaRecepcao)}" +
                $"?HorarioId={horario.Id}");


            return;
        }


        if (
            horario.Status ==
            "Bloqueado")
        {
            bool confirmar =
                await DisplayAlertAsync(
                    "Liberar horário",

                    $"Deseja liberar o horário das " +
                    $"{horario.HorarioFormatado}?\n\n" +

                    $"Motivo do bloqueio: " +
                    $"{horario.MotivoBloqueio}",

                    "LIBERAR",
                    "CANCELAR");


            if (!confirmar)
                return;


            horario.Status =
                "Disponível";


            horario.MotivoBloqueio =
                string.Empty;


            CarregarAgenda();


            await DisplayAlertAsync(
                "Horário liberado",
                "O horário foi liberado com sucesso.",
                "OK");
        }
    }
}