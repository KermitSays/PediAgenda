namespace PediAgenda.Views.Usuarios.Medico;

public partial class BloquearHorario : ContentPage
{
    public BloquearHorario()
    {
        InitializeComponent();

        DataInicialPicker.Date = DateTime.Today;
        DataFinalPicker.Date = DateTime.Today;

        HorarioInicialPicker.Time = new TimeSpan(12, 0, 0);
        HorarioFinalPicker.Time = new TimeSpan(12, 0, 0);
    }

    private async void BloquearHorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        DateTime dataInicial = DataInicialPicker.Date ?? DateTime.Today;
        DateTime dataFinal = DataFinalPicker.Date ?? DateTime.Today;

        TimeSpan horarioInicial = HorarioInicialPicker.Time ?? new TimeSpan(12, 0, 0);
        TimeSpan horarioFinal = HorarioFinalPicker.Time ?? new TimeSpan(12, 0, 0);

        string motivo = MotivoEditor.Text?.Trim() ?? string.Empty;

        // Valida as datas
        if (dataFinal < dataInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "A Data Final não pode ser anterior à Data Inicial.",
                "OK");

            return;
        }

        // Valida os horários
        if (dataInicial == dataFinal &&
            horarioFinal <= horarioInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "O Horário Final deve ser posterior ao Horário Inicial.",
                "OK");

            return;
        }

        // Valida o motivo
        if (string.IsNullOrWhiteSpace(motivo))
        {
            await DisplayAlertAsync(
                "Atenção",
                "Informe o motivo do bloqueio.",
                "OK");

            return;
        }

        string periodo =
            $"{dataInicial:dd/MM/yyyy} às {horarioInicial:hh\\:mm} " +
            $"até {dataFinal:dd/MM/yyyy} às {horarioFinal:hh\\:mm}";

        bool confirmar = await DisplayAlertAsync(
            "Confirmar bloqueio",
            $"Deseja bloquear o horário?\n\n" +
            $"Período: {periodo}\n\n" +
            $"Motivo: {motivo}",
            "Bloquear",
            "Cancelar");

        if (!confirmar)
            return;

        // Salva o bloqueio
        BloqueiosMedico.Bloqueios.Add(
            new BloqueioHorario
            {
                DataInicial = dataInicial,
                DataFinal = dataFinal,
                HorarioInicial = horarioInicial,
                HorarioFinal = horarioFinal,
                Motivo = motivo
            });

        await DisplayAlertAsync(
            "Horário bloqueado",
            "O horário foi bloqueado com sucesso.",
            "OK");

        await Shell.Current.GoToAsync("..");
    }

    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}