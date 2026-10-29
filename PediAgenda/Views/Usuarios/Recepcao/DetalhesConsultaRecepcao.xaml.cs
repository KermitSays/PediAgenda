using System.Globalization;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(ConsultaId), "ConsultaId")]
public partial class DetalhesConsultaRecepcao : ContentPage
{
    private int _idConsulta;
    private ConsultaRecepcaoRespostaApi? _consulta;
    private bool _ocupado;

    public string ConsultaId
    {
        set => _idConsulta = int.TryParse(value, out var id) ? id : 0;
    }

    public DetalhesConsultaRecepcao()
    {
        InitializeComponent();
        ConfirmarButton.IsEnabled = false;
        CancelarButton.IsEnabled = false;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AtualizarConsultaAsync();
    }

    private async Task AtualizarConsultaAsync()
    {
        if (_ocupado) return;
        if (_idConsulta <= 0)
        {
            await DisplayAlertAsync("Consulta", "Não foi possível identificar a consulta.", "OK");
            return;
        }

        _ocupado = true;
        ConfirmarButton.IsEnabled = false;
        CancelarButton.IsEnabled = false;
        try
        {
            _consulta = await RecepcaoGestaoConsultasApi.BuscarAsync(_idConsulta);
            PacienteLabel.Text = _consulta.Paciente;
            ResponsavelLabel.Text = $"Responsável: {_consulta.Responsavel}";
            MedicoLabel.Text = _consulta.Medico;
            DataLabel.Text = DateTime.TryParseExact(_consulta.Data, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d.ToString("dd/MM/yyyy") : _consulta.Data;
            HorarioLabel.Text = _consulta.Inicio;
            StatusLabel.Text = TextoStatus(_consulta.Status);
        }
        catch (Exception ex)
        {
            _consulta = null;
            PacienteLabel.Text = "Não foi possível carregar";
            ResponsavelLabel.Text = "";
            await DisplayAlertAsync("Erro ao consultar", ex.Message, "OK");
        }
        finally
        {
            _ocupado = false;
            AtualizarBotoes();
        }
    }

    private static string TextoStatus(string? status) => status?.ToUpperInvariant() switch
    {
        "AGENDADA" => "Por confirmar",
        "CONFIRMADA" => "Confirmada",
        "CANCELADA" => "Cancelada",
        "REALIZADA" => "Realizada",
        _ => status ?? "Indisponível"
    };

    private DateTime InicioConsulta()
    {
        if (_consulta is null ||
            !DateTime.TryParseExact(_consulta.Data, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var data) ||
            !TimeSpan.TryParse(_consulta.Inicio, CultureInfo.InvariantCulture, out var hora))
            return DateTime.MinValue;
        return data.Date.Add(hora);
    }

    private void AtualizarBotoes()
    {
        var status = _consulta?.Status?.ToUpperInvariant();
        var inicio = InicioConsulta();
        var agora = DateTime.Now;
        bool ativa = status is "AGENDADA" or "CONFIRMADA";
        ConfirmarButton.IsEnabled = !_ocupado && status == "AGENDADA" && inicio > agora;
        ConfirmarButton.Text = status == "CONFIRMADA" ? "CONSULTA CONFIRMADA" : "CONFIRMAR CONSULTA";
        CancelarButton.IsEnabled = !_ocupado && ativa && inicio > agora;
    }

    private async void ConfirmarButton_Clicked(object sender, EventArgs e)
    {
        if (_ocupado || _consulta is null || !ConfirmarButton.IsEnabled) return;
        bool confirmou = await DisplayAlertAsync("Confirmar consulta",
            $"Confirmar a consulta de {_consulta.Paciente}?", "CONFIRMAR", "VOLTAR");
        if (!confirmou) return;

        _ocupado = true;
        ConfirmarButton.IsEnabled = false;
        CancelarButton.IsEnabled = false;
        try
        {
            await RecepcaoGestaoConsultasApi.ConfirmarAsync(_consulta.Id);
            await DisplayAlertAsync("Consulta confirmada",
                "A confirmação foi registrada no banco de dados.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Não foi possível confirmar", ex.Message, "OK");
        }
        finally { _ocupado = false; }
        await AtualizarConsultaAsync();
    }

    private async void CancelarButton_Clicked(object sender, EventArgs e)
    {
        if (_ocupado || _consulta is null || !CancelarButton.IsEnabled) return;
        bool cancelar = await DisplayAlertAsync("Cancelar consulta",
            $"Deseja cancelar a consulta de {_consulta.Paciente}?\n\n" +
            "O horário será liberado. Para marcar outra consulta, será necessário um novo agendamento.",
            "CANCELAR CONSULTA", "VOLTAR");
        if (!cancelar) return;

        _ocupado = true;
        ConfirmarButton.IsEnabled = false;
        CancelarButton.IsEnabled = false;
        try
        {
            await RecepcaoGestaoConsultasApi.CancelarAsync(_consulta.Id);
            await DisplayAlertAsync("Consulta cancelada",
                "O cancelamento foi registrado no Aiven e o horário voltou a ficar disponível.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Não foi possível cancelar", ex.Message, "OK");
        }
        finally { _ocupado = false; }
        await AtualizarConsultaAsync();
    }

    private async void VoltarButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
