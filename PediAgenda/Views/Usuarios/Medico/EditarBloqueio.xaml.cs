namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(IdBloqueio), "IdBloqueio")]
public partial class EditarBloqueio : ContentPage
{
    private string idBloqueio = string.Empty;

    private BloqueioHorario? bloqueioAtual;

    public string IdBloqueio
    {
        get => idBloqueio;

        set
        {
            idBloqueio = value;

            if (DataInicialPicker != null)
                CarregarBloqueio();
        }
    }

    public EditarBloqueio()
    {
        InitializeComponent();
    }

    private void CarregarBloqueio()
    {
        if (!Guid.TryParse(IdBloqueio, out Guid id))
            return;

        bloqueioAtual =
            BloqueiosMedico.Bloqueios
                .FirstOrDefault(bloqueio => bloqueio.Id == id);

        if (bloqueioAtual == null)
            return;

        DataInicialPicker.Date =
            bloqueioAtual.DataInicial;

        DataFinalPicker.Date =
            bloqueioAtual.DataFinal;

        HorarioInicialPicker.Time =
            bloqueioAtual.HorarioInicial;

        HorarioFinalPicker.Time =
            bloqueioAtual.HorarioFinal;

        MotivoEditor.Text =
            bloqueioAtual.Motivo;
    }

    private async void SalvarAlteracaoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (bloqueioAtual == null)
        {
            await DisplayAlertAsync(
                "Atenção",
                "Não foi possível localizar o bloqueio.",
                "OK");

            return;
        }

        DateTime dataInicial =
            DataInicialPicker.Date ?? DateTime.Today;

        DateTime dataFinal =
            DataFinalPicker.Date ?? DateTime.Today;

        TimeSpan horarioInicial =
            HorarioInicialPicker.Time ??
            new TimeSpan(12, 0, 0);

        TimeSpan horarioFinal =
            HorarioFinalPicker.Time ??
            new TimeSpan(12, 0, 0);

        string motivo =
            MotivoEditor.Text?.Trim() ?? string.Empty;

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

        bool confirmar = await DisplayAlertAsync(
            "Salvar alteração",
            "Deseja salvar as alterações deste bloqueio?",
            "Salvar",
            "Cancelar");

        if (!confirmar)
            return;

        bloqueioAtual.DataInicial = dataInicial;
        bloqueioAtual.DataFinal = dataFinal;
        bloqueioAtual.HorarioInicial = horarioInicial;
        bloqueioAtual.HorarioFinal = horarioFinal;
        bloqueioAtual.Motivo = motivo;

        await DisplayAlertAsync(
            "Alteração salva",
            "O bloqueio foi atualizado com sucesso.",
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