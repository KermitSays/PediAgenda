using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class MedicosRecepcao : ContentPage
{
    public ObservableCollection<HorarioAgendaRecepcao>
        HorariosFiltrados
    { get; set; } = new();


    public MedicosRecepcao()
    {
        InitializeComponent();

        BindingContext = this;

        CarregarMedicos();

        // Data dos mocks atuais
        DataPicker.Date = new DateTime(2026, 10, 5);

        CarregarAgenda();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        CarregarAgenda();
    }


    private void CarregarMedicos()
    {
        MedicoPicker.Items.Clear();

        foreach (var medico in AgendaMedicaRecepcaoDados.Medicos)
        {
            MedicoPicker.Items.Add(medico);
        }

        if (MedicoPicker.Items.Count > 0)
        {
            MedicoPicker.SelectedIndex = 0;
        }
    }


    private void Filtro_Changed(
        object sender,
        EventArgs e)
    {
        CarregarAgenda();
    }


    private void DataPicker_DateSelected(
        object sender,
        DateChangedEventArgs e)
    {
        CarregarAgenda();
    }


    private void CarregarAgenda()
    {
        string medicoSelecionado =
            MedicoPicker.SelectedItem?.ToString() ?? string.Empty;

        DateTime dataSelecionada =
            DataPicker.Date ?? DateTime.Today;


        var horarios =
            AgendaMedicaRecepcaoDados.Horarios
                .Where(h =>
                    h.Medico == medicoSelecionado &&
                    h.Data.Date == dataSelecionada.Date)
                .OrderBy(h => h.Horario)
                .ToList();


        HorariosFiltrados.Clear();

        foreach (var horario in horarios)
        {
            HorariosFiltrados.Add(horario);
        }


        QuantidadeHorariosLabel.Text =
            $"{HorariosFiltrados.Count} horário(s)";
    }


    private async void HorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is not Button botao ||
            botao.CommandParameter is not HorarioAgendaRecepcao horario)
        {
            return;
        }


        if (horario.Status == "Disponível")
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


        if (horario.Status == "Agendado")
        {
            await Shell.Current.GoToAsync(
                $"{nameof(DetalhesConsultaRecepcao)}?HorarioId={horario.Id}");

            return;
        }


        if (horario.Status == "Bloqueado")
        {
            bool confirmar =
                await DisplayAlertAsync(
                    "Liberar horário",
                    $"Deseja liberar o horário das {horario.HorarioFormatado}?\n\n" +
                    $"Motivo do bloqueio: {horario.MotivoBloqueio}",
                    "LIBERAR",
                    "CANCELAR");


            if (!confirmar)
                return;


            horario.Status = "Disponível";
            horario.MotivoBloqueio = string.Empty;


            // Recarrega a lista para refletir a alteração
            CarregarAgenda();


            await DisplayAlertAsync(
                "Horário liberado",
                "O horário foi liberado com sucesso.",
                "OK");
        }
    }
}