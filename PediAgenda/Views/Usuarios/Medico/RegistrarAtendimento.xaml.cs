namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(
    nameof(IdConsulta),
    "IdConsulta")]
public partial class RegistrarAtendimento : ContentPage
{
    private Guid idConsulta;


    private ConsultaMedico?
        consultaAtual;


    private PacienteMedicoItem?
        pacienteAtual;


    public string IdConsulta
    {
        set
        {
            if (
                Guid.TryParse(
                    value,
                    out Guid id))
            {
                idConsulta =
                    id;


                if (PacienteLabel != null)
                {
                    CarregarConsulta();
                }
            }
        }
    }


    public RegistrarAtendimento()
    {
        InitializeComponent();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        CarregarConsulta();
    }


    // =============================================
    // CARREGA CONSULTA
    // =============================================

    private void CarregarConsulta()
    {
        FinalizarAtendimentoButton.IsEnabled =
            false;


        if (
            idConsulta ==
            Guid.Empty)
        {
            return;
        }


        consultaAtual =
            ConsultasMedicoDados
                .ObterConsultaPorId(
                    idConsulta);


        if (consultaAtual == null)
        {
            PacienteLabel.Text =
                "Consulta não encontrada";


            return;
        }


        if (!consultaAtual.IdPaciente.HasValue)
        {
            PacienteLabel.Text =
                consultaAtual.Paciente;


            ResponsavelLabel.Text =
                "Paciente ainda não vinculado";


            DataLabel.Text =
                consultaAtual.Data
                    .ToString(
                        "dd/MM/yyyy");


            HorarioLabel.Text =
                consultaAtual.HorarioTexto;


            TipoConsultaLabel.Text =
                consultaAtual.TipoConsulta;


            return;
        }


        pacienteAtual =
            MedicoDados
                .ObterPacientePorId(
                    consultaAtual.IdPaciente.Value);


        if (pacienteAtual == null)
        {
            PacienteLabel.Text =
                consultaAtual.Paciente;


            ResponsavelLabel.Text =
                "Paciente não encontrado";


            return;
        }


        PacienteLabel.Text =
            pacienteAtual.Nome;


        ResponsavelLabel.Text =
            pacienteAtual.Responsavel;


        DataLabel.Text =
            consultaAtual.Data
                .ToString(
                    "dd/MM/yyyy");


        HorarioLabel.Text =
            consultaAtual.HorarioTexto;


        TipoConsultaLabel.Text =
            consultaAtual.TipoConsulta;


        bool podeRegistrar =
            !consultaAtual.Status.Equals(
                "Realizada",
                StringComparison.OrdinalIgnoreCase)

            &&

            !consultaAtual.Status.Equals(
                "Cancelado",
                StringComparison.OrdinalIgnoreCase)

            &&

            !consultaAtual.Status.Equals(
                "Cancelada",
                StringComparison.OrdinalIgnoreCase)

            &&

            consultaAtual.Data.Date <=
                DateTime.Today;


        FinalizarAtendimentoButton.IsEnabled =
            podeRegistrar;
    }


    // =============================================
    // FINALIZA ATENDIMENTO
    // =============================================

    private async void FinalizarAtendimentoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            consultaAtual == null ||

            pacienteAtual == null)
        {
            await DisplayAlertAsync(
                "Atendimento indisponível",
                "Não foi possível localizar os dados desta consulta.",
                "OK");


            return;
        }


        // NÃO PERMITE REGISTRAR CONSULTA FUTURA

        if (
            consultaAtual.Data.Date >
            DateTime.Today)
        {
            await DisplayAlertAsync(
                "Atendimento ainda indisponível",
                "O atendimento só pode ser registrado no dia da consulta.",
                "OK");


            return;
        }


        // NÃO PERMITE CONSULTA CANCELADA

        if (
            consultaAtual.Status.Equals(
                "Cancelado",
                StringComparison.OrdinalIgnoreCase)

            ||

            consultaAtual.Status.Equals(
                "Cancelada",
                StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Consulta cancelada",
                "Não é possível registrar atendimento em uma consulta cancelada.",
                "OK");


            return;
        }


        // VERIFICA SE JÁ EXISTE PRONTUÁRIO
        // PARA ESTA CONSULTA

        ConsultaHistoricoMedicoItem?
            registroExistente =
                MedicoDados
                    .ObterHistoricoPorConsulta(
                        consultaAtual.Id);


        if (
            registroExistente !=
            null

            ||

            consultaAtual.Status.Equals(
                "Realizada",
                StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Atendimento já registrado",
                "Esta consulta já possui um atendimento registrado no prontuário.",
                "OK");


            return;
        }


        string observacoes =
            ObservacoesEditor.Text?.Trim()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(
            observacoes))
        {
            await DisplayAlertAsync(
                "Observações obrigatórias",
                "Digite as observações do atendimento antes de finalizar.",
                "OK");


            return;
        }


        bool confirmar =
            await DisplayAlertAsync(
                "Finalizar atendimento",

                $"Deseja finalizar o atendimento de " +
                $"{pacienteAtual.Nome}?\n\n" +
                "A consulta será marcada como realizada " +
                "e o registro será adicionado ao prontuário.",

                "FINALIZAR",
                "VOLTAR");


        if (!confirmar)
            return;


        // CRIA O REGISTRO NO PRONTUÁRIO

        pacienteAtual.Historico.Add(
            new ConsultaHistoricoMedicoItem
            {
                IdHistorico =
                    MedicoDados
                        .ProximoIdHistorico(),

                IdConsulta =
                    consultaAtual.Id,

                TipoConsulta =
                    consultaAtual.TipoConsulta,

                Data =
                    consultaAtual.Data,

                Observacoes =
                    observacoes
            });


        // ALTERA O STATUS DA CONSULTA

        consultaAtual.Status =
            "Realizada";


        await DisplayAlertAsync(
            "Atendimento registrado",
            "A consulta foi marcada como realizada e o prontuário foi atualizado.",
            "OK");


        await Shell.Current.GoToAsync(
            "..");
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