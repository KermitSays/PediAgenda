
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(HorarioId), "HorarioId")]
public partial class DetalhesConsultaRecepcao : ContentPage
{
    private int _horarioId;

    private PacienteRecepcaoItem? _paciente;
    private ConsultaPacienteRecepcaoItem? _consulta;

    // ============================================
    // IDENTIFICAÇÃO DO HORÁRIO
    // ============================================

    public string HorarioId
    {
        set
        {
            if (int.TryParse(value, out int id) && id > 0)
            {
                _horarioId = id;

                if (PacienteLabel != null)
                {
                    CarregarConsulta();
                }
            }
        }
    }

    public DetalhesConsultaRecepcao()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CarregarConsulta();
    }

    // ============================================
    // CARREGAR CONSULTA
    // ============================================

    private void CarregarConsulta()
    {
        _paciente = null;
        _consulta = null;

        if (_horarioId <= 0)
        {
            DesabilitarAcoes();
            return;
        }

        _paciente = PacientesRecepcaoDados.Pacientes
            .FirstOrDefault(p =>
                p.Consultas.Any(c =>
                    c.HorarioId == _horarioId));

        if (_paciente == null)
        {
            PacienteLabel.Text = "Paciente não localizado";
            ResponsavelLabel.Text = string.Empty;

            DesabilitarAcoes();
            return;
        }

        _consulta = _paciente.Consultas
            .FirstOrDefault(c =>
                c.HorarioId == _horarioId);

        if (_consulta == null)
        {
            DesabilitarAcoes();
            return;
        }

        // DADOS DO PACIENTE
        PacienteLabel.Text = _paciente.Nome;

        ResponsavelLabel.Text =
            $"Responsável: {_paciente.Responsavel}";

        // DADOS DA CONSULTA
        MedicoLabel.Text = _consulta.Medico;

        DataLabel.Text =
            _consulta.Data.ToString("dd/MM/yyyy");

        HorarioLabel.Text =
            _consulta.Horario.ToString(@"hh\:mm");

        StatusLabel.Text = _consulta.Status;

        AtualizarBotoes();
    }

    // ============================================
    // STATUS E BOTÕES
    // ============================================

    private static bool EhStatus(
        string? status,
        params string[] valores)
    {
        return valores.Any(valor =>
            string.Equals(
                status?.Trim(),
                valor,
                StringComparison.OrdinalIgnoreCase));
    }

    private void AtualizarBotoes()
    {
        if (_consulta == null)
        {
            DesabilitarAcoes();
            return;
        }

        bool cancelada = EhStatus(
            _consulta.Status,
            "Cancelado",
            "Cancelada");

        bool realizada = EhStatus(
            _consulta.Status,
            "Realizado",
            "Realizada");

        bool confirmada = EhStatus(
            _consulta.Status,
            "Confirmado",
            "Confirmada");

        bool consultaAtiva =
            !cancelada &&
            !realizada &&
            _consulta.HorarioId.HasValue;

        // Só permite confirmar consultas ativas
        // que ainda não foram confirmadas.
        ConfirmarButton.IsEnabled =
            consultaAtiva && !confirmada;

        ConfirmarButton.Text =
            confirmada
                ? "CONSULTA CONFIRMADA"
                : "CONFIRMAR CONSULTA";

        // Cancelamento continua disponível
        // para consultas ativas.
        CancelarButton.IsEnabled = consultaAtiva;
    }

    private void DesabilitarAcoes()
    {
        ConfirmarButton.IsEnabled = false;
        CancelarButton.IsEnabled = false;
    }

    // ============================================
    // CONFIRMAR CONSULTA
    // ============================================

    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_consulta == null ||
            _paciente == null ||
            !ConfirmarButton.IsEnabled)
        {
            return;
        }

        bool confirmar = await DisplayAlertAsync(
            "Confirmar consulta",
            $"Confirmar a consulta de {_paciente.Nome}?",
            "CONFIRMAR",
            "VOLTAR");

        if (!confirmar)
            return;

        // Confirmação local.
        _consulta.Status = "Confirmado";

        StatusLabel.Text = _consulta.Status;

        AtualizarBotoes();

        await DisplayAlertAsync(
            "Consulta confirmada",
            "A consulta foi confirmada com sucesso.",
            "OK");
    }

    // ============================================
    // CANCELAR CONSULTA
    // ============================================

    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_consulta == null ||
            _paciente == null ||
            !_consulta.HorarioId.HasValue ||
            !CancelarButton.IsEnabled)
        {
            return;
        }

        bool cancelar = await DisplayAlertAsync(
            "Cancelar consulta",

            $"Deseja cancelar a consulta de " +
            $"{_paciente.Nome} " +
            $"no dia {_consulta.Data:dd/MM/yyyy} às " +
            $"{_consulta.Horario:hh\\:mm}?\n\n" +
            "Para marcar outra consulta, será necessário " +
            "realizar um novo agendamento.",

            "CANCELAR CONSULTA",
            "VOLTAR");

        if (!cancelar)
            return;

        int idHorario = _consulta.HorarioId.Value;

        // ========================================
        // LIBERAR HORÁRIO COMPARTILHADO
        // ========================================

        HorarioAgendamentoDados.LiberarHorario(
            idHorario);

        // ========================================
        // MANTER CONSULTA NO HISTÓRICO
        // ========================================

        _consulta.Status = "Cancelado";

        // A consulta não ocupa mais o horário.
        _consulta.HorarioId = null;

        StatusLabel.Text = _consulta.Status;

        AtualizarBotoes();

        await DisplayAlertAsync(
            "Consulta cancelada",

            $"A consulta de {_paciente.Nome} " +
            "foi cancelada e o horário foi liberado.\n\n" +
            "Caso necessário, realize um novo agendamento.",

            "OK");
    }

    // ============================================
    // VOLTAR
    // ============================================

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
