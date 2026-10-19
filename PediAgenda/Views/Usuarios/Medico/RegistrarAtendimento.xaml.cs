
namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(IdConsulta), "IdConsulta")]
public partial class RegistrarAtendimento : ContentPage
{
    private Guid idConsulta;

    private ConsultaMedico? consultaAtual;

    private PacienteMedicoItem? pacienteAtual;

    public string IdConsulta
    {
        set
        {
            if (Guid.TryParse(value, out Guid id))
            {
                idConsulta = id;

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

    // ============================================
    // VERIFICAR SE A CONSULTA ESTÁ CONFIRMADA
    // ============================================

    private static bool EhConsultaConfirmada(
        string? status)
    {
        return
            string.Equals(
                status?.Trim(),
                "Confirmada",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                status?.Trim(),
                "Confirmado",
                StringComparison.OrdinalIgnoreCase);
    }

    // ============================================
    // ABERTURA DA TELA
    // ============================================

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        CarregarConsulta();

        // Proteção adicional:
        // impede acesso por outra rota.

        if (consultaAtual != null &&
            !EhConsultaConfirmada(consultaAtual.Status))
        {
            await DisplayAlertAsync(
                "Atendimento indisponível",
                "Esta consulta não está confirmada. " +
                "O registro do atendimento ainda não está disponível.",
                "Entendi");

            await Shell.Current.GoToAsync("..");
        }
    }

    // ============================================
    // CARREGAR CONSULTA
    // ============================================

    private void CarregarConsulta()
    {
        FinalizarAtendimentoButton.IsEnabled = false;

        consultaAtual = null;
        pacienteAtual = null;

        if (idConsulta == Guid.Empty)
        {
            return;
        }

        consultaAtual =
            ConsultasMedicoDados.ObterConsultaPorId(
                idConsulta);

        if (consultaAtual == null)
        {
            PacienteLabel.Text =
                "Consulta não encontrada";

            return;
        }

        // ========================================
        // PACIENTE NÃO VINCULADO
        // ========================================

        if (!consultaAtual.IdPaciente.HasValue)
        {
            PacienteLabel.Text =
                consultaAtual.Paciente;

            ResponsavelLabel.Text =
                "Paciente ainda não vinculado";

            DataLabel.Text =
                consultaAtual.Data.ToString("dd/MM/yyyy");

            HorarioLabel.Text =
                consultaAtual.HorarioTexto;

            TipoConsultaLabel.Text =
                consultaAtual.TipoConsulta;

            return;
        }

        // ========================================
        // BUSCAR PACIENTE
        // ========================================

        pacienteAtual =
            MedicoDados.ObterPacientePorId(
                consultaAtual.IdPaciente.Value);

        if (pacienteAtual == null)
        {
            PacienteLabel.Text =
                consultaAtual.Paciente;

            ResponsavelLabel.Text =
                "Paciente não encontrado";

            return;
        }

        // ========================================
        // EXIBIR DADOS DA CONSULTA
        // ========================================

        PacienteLabel.Text =
            pacienteAtual.Nome;

        ResponsavelLabel.Text =
            pacienteAtual.Responsavel;

        DataLabel.Text =
            consultaAtual.Data.ToString("dd/MM/yyyy");

        HorarioLabel.Text =
            consultaAtual.HorarioTexto;

        TipoConsultaLabel.Text =
            consultaAtual.TipoConsulta;

        // ========================================
        // HABILITAR FINALIZAÇÃO
        // SOMENTE SE CONFIRMADA
        // ========================================

        bool podeRegistrar =
            EhConsultaConfirmada(consultaAtual.Status)
            &&
            consultaAtual.Data.Date <= DateTime.Today;

        FinalizarAtendimentoButton.IsEnabled =
            podeRegistrar;
    }

    // ============================================
    // FINALIZAR ATENDIMENTO
    // ============================================

    private async void FinalizarAtendimentoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (consultaAtual == null ||
            pacienteAtual == null)
        {
            await DisplayAlertAsync(
                "Atendimento indisponível",
                "Não foi possível localizar os dados desta consulta.",
                "OK");

            return;
        }

        // ========================================
        // NÃO PERMITE CONSULTA FUTURA
        // ========================================

        if (consultaAtual.Data.Date > DateTime.Today)
        {
            await DisplayAlertAsync(
                "Atendimento ainda indisponível",
                "O atendimento só pode ser registrado no dia da consulta.",
                "OK");

            return;
        }

        // ========================================
        // NÃO PERMITE CONSULTA CANCELADA
        // ========================================

        if (
            string.Equals(
                consultaAtual.Status,
                "Cancelado",
                StringComparison.OrdinalIgnoreCase)
            ||
            string.Equals(
                consultaAtual.Status,
                "Cancelada",
                StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Consulta cancelada",
                "Não é possível registrar atendimento em uma consulta cancelada.",
                "OK");

            return;
        }

        // ========================================
        // NÃO PERMITE CONSULTA NÃO CONFIRMADA
        // NOVA VALIDAÇÃO
        // ========================================

        if (!EhConsultaConfirmada(consultaAtual.Status))
        {
            await DisplayAlertAsync(
                "Consulta não confirmada",
                "Não é possível finalizar um atendimento " +
                "sem a confirmação da consulta.",
                "OK");

            return;
        }

        // ========================================
        // VERIFICAR PRONTUÁRIO EXISTENTE
        // ========================================

        ConsultaHistoricoMedicoItem? registroExistente =
            MedicoDados.ObterHistoricoPorConsulta(
                consultaAtual.Id);

        if (
            registroExistente != null
            ||
            string.Equals(
                consultaAtual.Status,
                "Realizada",
                StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Atendimento já registrado",
                "Esta consulta já possui um atendimento registrado no prontuário.",
                "OK");

            return;
        }

        // ========================================
        // VALIDAR OBSERVAÇÕES
        // ========================================

        string observacoes =
            ObservacoesEditor.Text?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(observacoes))
        {
            await DisplayAlertAsync(
                "Observações obrigatórias",
                "Digite as observações do atendimento antes de finalizar.",
                "OK");

            return;
        }

        // ========================================
        // CONFIRMAR FINALIZAÇÃO
        // ========================================

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
        {
            return;
        }

        // ========================================
        // CRIAR REGISTRO NO PRONTUÁRIO LOCAL
        // ========================================

        pacienteAtual.Historico.Add(
            new ConsultaHistoricoMedicoItem
            {
                IdHistorico =
                    MedicoDados.ProximoIdHistorico(),

                IdConsulta =
                    consultaAtual.Id,

                TipoConsulta =
                    consultaAtual.TipoConsulta,

                Data =
                    consultaAtual.Data,

                Observacoes =
                    observacoes
            });

        // ========================================
        // MARCAR CONSULTA COMO REALIZADA
        // ========================================

        consultaAtual.Status = "Realizada";

        await DisplayAlertAsync(
            "Atendimento registrado",
            "A consulta foi marcada como realizada e o prontuário foi atualizado.",
            "OK");

        await Shell.Current.GoToAsync("..");
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
