using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(HorarioId), "HorarioId")]
public partial class DetalhesConsultaRecepcao : ContentPage
{
    private int _horarioId;

    private PacienteRecepcaoItem?
        _paciente;

    private ConsultaPacienteRecepcaoItem?
        _consulta;


    public string HorarioId
    {
        set
        {
            if (
                int.TryParse(
                    value,
                    out int id))
            {
                _horarioId =
                    id;


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


    // =============================================
    // CARREGA A CONSULTA
    // =============================================

    private void CarregarConsulta()
    {
        if (_horarioId <= 0)
            return;


        _paciente =
            PacientesRecepcaoDados.Pacientes
                .FirstOrDefault(p =>
                    p.Consultas.Any(c =>
                        c.HorarioId ==
                        _horarioId));


        if (_paciente == null)
        {
            PacienteLabel.Text =
                "Paciente não localizado";


            ResponsavelLabel.Text =
                string.Empty;


            DesabilitarAcoes();


            return;
        }


        _consulta =
            _paciente.Consultas
                .FirstOrDefault(c =>
                    c.HorarioId ==
                    _horarioId);


        if (_consulta == null)
        {
            DesabilitarAcoes();


            return;
        }


        PacienteLabel.Text =
            _paciente.Nome;


        ResponsavelLabel.Text =
            $"Responsável: " +
            $"{_paciente.Responsavel}";


        MedicoLabel.Text =
            _consulta.Medico;


        DataLabel.Text =
            _consulta.Data.ToString(
                "dd/MM/yyyy");


        HorarioLabel.Text =
            _consulta.Horario.ToString(
                @"hh\:mm");


        StatusLabel.Text =
            _consulta.Status;


        AtualizarBotoes();
    }


    // =============================================
    // BOTÕES
    // =============================================

    private void AtualizarBotoes()
    {
        if (_consulta == null)
        {
            DesabilitarAcoes();


            return;
        }


        bool cancelada =
            _consulta.Status.Equals(
                "Cancelado",
                StringComparison.OrdinalIgnoreCase)

            ||

            _consulta.Status.Equals(
                "Cancelada",
                StringComparison.OrdinalIgnoreCase);


        bool realizada =
            _consulta.Status.Equals(
                "Realizado",
                StringComparison.OrdinalIgnoreCase)

            ||

            _consulta.Status.Equals(
                "Realizada",
                StringComparison.OrdinalIgnoreCase);


        bool consultaAtiva =
            !cancelada

            &&

            !realizada

            &&

            _consulta.HorarioId.HasValue;


        ConfirmarButton.IsEnabled =
            consultaAtiva;


        ReagendarButton.IsEnabled =
            consultaAtiva;


        CancelarButton.IsEnabled =
            consultaAtiva;


        if (
            _consulta.Status.Equals(
                "Confirmado",
                StringComparison.OrdinalIgnoreCase)

            ||

            _consulta.Status.Equals(
                "Confirmada",
                StringComparison.OrdinalIgnoreCase))
        {
            ConfirmarButton.IsEnabled =
                false;


            ConfirmarButton.Text =
                "CONSULTA CONFIRMADA";
        }
        else
        {
            ConfirmarButton.Text =
                "CONFIRMAR CONSULTA";
        }
    }


    private void DesabilitarAcoes()
    {
        ConfirmarButton.IsEnabled =
            false;


        ReagendarButton.IsEnabled =
            false;


        CancelarButton.IsEnabled =
            false;
    }


    // =============================================
    // CONFIRMAR CONSULTA
    // =============================================

    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            _consulta == null

            ||

            _paciente == null)
        {
            return;
        }


        bool confirmar =
            await DisplayAlertAsync(
                "Confirmar consulta",
                $"Confirmar a consulta de {_paciente.Nome}?",
                "CONFIRMAR",
                "VOLTAR");


        if (!confirmar)
            return;


        _consulta.Status =
            "Confirmado";


        StatusLabel.Text =
            _consulta.Status;


        AtualizarBotoes();


        await DisplayAlertAsync(
            "Consulta confirmada",
            "A consulta foi confirmada com sucesso.",
            "OK");
    }


    // =============================================
    // REAGENDAR
    // =============================================

    private async void ReagendarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            _consulta == null

            ||

            _paciente == null

            ||

            !_consulta.HorarioId.HasValue)
        {
            return;
        }


        await Shell.Current.GoToAsync(
            $"{nameof(AgendarConsultaRecepcao)}" +
            $"?PacienteId={_paciente.Id}" +
            $"&HorarioId={_consulta.HorarioId.Value}");
    }


    // =============================================
    // CANCELAR
    // =============================================

    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            _consulta == null

            ||

            _paciente == null

            ||

            !_consulta.HorarioId.HasValue)
        {
            return;
        }


        bool cancelar =
            await DisplayAlertAsync(
                "Cancelar consulta",

                $"Deseja cancelar a consulta de " +
                $"{_paciente.Nome} " +
                $"no dia {_consulta.Data:dd/MM/yyyy} às " +
                $"{_consulta.Horario:hh\\:mm}?",

                "CANCELAR CONSULTA",
                "VOLTAR");


        if (!cancelar)
            return;


        int idHorario =
            _consulta.HorarioId.Value;


        // =========================================
        // LIBERA A FONTE COMPARTILHADA
        // =========================================

        HorarioAgendamentoDados
            .LiberarHorario(
                idHorario);


        // =========================================
        // MANTÉM A CONSULTA NO HISTÓRICO
        // =========================================

        _consulta.Status =
            "Cancelado";


        // O horário deixou de ser uma reserva ativa.

        _consulta.HorarioId =
            null;


        StatusLabel.Text =
            _consulta.Status;


        AtualizarBotoes();


        await DisplayAlertAsync(
            "Consulta cancelada",

            $"A consulta de {_paciente.Nome} " +
            "foi cancelada e o horário foi liberado.",

            "OK");
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