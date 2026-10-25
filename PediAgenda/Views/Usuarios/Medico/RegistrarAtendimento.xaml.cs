
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class RegistrarAtendimento : ContentPage, IQueryAttributable
{
    private AtendimentoMedicoApi? atendimentoApi;
    private bool salvando;
    private int usuarioApi;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        atendimentoApi = query.TryGetValue("AtendimentoApi", out var valor)
            ? valor as AtendimentoMedicoApi : null;
        usuarioApi = SessaoUsuario.IdUsuario;
        if (MedicoApi.ModoTeste && query.TryGetValue("IdConsulta", out var id))
            IdConsulta = id?.ToString() ?? string.Empty;
    }

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

        if (!MedicoApi.ModoTeste)
        {
            await CarregarAtendimentoApiAsync();
            return;
        }

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
        if (!MedicoApi.ModoTeste) return;

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
        if (!MedicoApi.ModoTeste)
        {
            await FinalizarAtendimentoApiAsync();
            return;
        }

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

    private async Task CarregarAtendimentoApiAsync()
    {
        FinalizarAtendimentoButton.IsEnabled = false;
        try
        {
            if (atendimentoApi?.Horario.Consulta == null || usuarioApi != SessaoUsuario.IdUsuario)
                throw new InvalidOperationException("Abra uma consulta pela agenda do médico conectado.");

            var horarios = await MedicoApi.ObterAgendaAsync(atendimentoApi.Data);
            if (usuarioApi != SessaoUsuario.IdUsuario) return;
            var horario = horarios.FirstOrDefault(h => h.Consulta?.Id == atendimentoApi.Horario.Consulta.Id)
                ?? throw new InvalidOperationException("A consulta não está mais disponível na agenda. Volte e atualize a lista.");
            atendimentoApi = atendimentoApi with { Horario = horario };
            var consulta = horario.Consulta!;
            PacienteLabel.Text = consulta.Paciente.Nome;
            ResponsavelLabel.Text = consulta.Responsavel.Nome;
            DataLabel.Text = atendimentoApi.Data.ToString("dd/MM/yyyy");
            HorarioLabel.Text = horario.Inicio;
            TipoConsultaLabel.Text = consulta.TipoAtendimento;
            string? impedimento = ImpedimentoAtendimentoApi();
            if (impedimento != null)
                await DisplayAlertAsync("Atendimento indisponível", impedimento, "OK");
            else
                FinalizarAtendimentoButton.IsEnabled = !salvando;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Atendimento", ex is InvalidOperationException
                ? ex.Message : "Não foi possível carregar a consulta. Volte à agenda e tente novamente.", "OK");
        }
    }

    private string? ImpedimentoAtendimentoApi()
    {
        var consulta = atendimentoApi?.Horario.Consulta;
        if (consulta == null || usuarioApi != SessaoUsuario.IdUsuario)
            return "Abra a consulta novamente pela agenda.";
        if (StatusConsulta.EhRealizada(consulta.Status))
            return "Esta consulta já possui um atendimento registrado.";
        if (StatusConsulta.EhCancelada(consulta.Status))
            return "Esta consulta foi cancelada.";
        // Preserva a validação já existente na tela; a API também valida o registro.
        if (!StatusConsulta.EhConfirmada(consulta.Status))
            return "A consulta precisa estar confirmada para registrar o atendimento nesta tela.";
        if (atendimentoApi!.Data.Date > DateTime.Today)
            return "O atendimento ainda não está disponível para esta data.";
        return null;
    }

    private async Task FinalizarAtendimentoApiAsync()
    {
        if (salvando) return;
        string? impedimento = ImpedimentoAtendimentoApi();
        if (impedimento != null)
        {
            await DisplayAlertAsync("Atendimento indisponível", impedimento, "OK");
            return;
        }
        string observacoes = ObservacoesEditor.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(observacoes))
        {
            await DisplayAlertAsync("Observações obrigatórias",
                "Digite as observações do atendimento antes de finalizar.", "OK");
            return;
        }
        salvando = true;
        FinalizarAtendimentoButton.IsEnabled = false;
        bool registrado = false;
        try
        {
            bool confirmar = await DisplayAlertAsync("Finalizar atendimento",
                $"Deseja finalizar o atendimento de {atendimentoApi!.Horario.Consulta!.Paciente.Nome}?\n\nA consulta será marcada como realizada e o prontuário será salvo.",
                "FINALIZAR", "VOLTAR");
            if (!confirmar) return;
            if (usuarioApi != SessaoUsuario.IdUsuario)
                throw new InvalidOperationException("Sua sessão mudou. Abra a consulta novamente pela agenda.");
            await MedicoApi.RegistrarAtendimentoAsync(atendimentoApi.Horario.Consulta.Id, observacoes);
            registrado = true;
            ObservacoesEditor.IsEnabled = false;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Não foi possível confirmar o registro",
                (ex is InvalidOperationException ? ex.Message : "Verifique a conexão e atualize a agenda.")
                + "\n\nSuas observações foram mantidas. Em caso de falha de conexão, confira a agenda antes de repetir o envio.", "OK");
        }
        finally
        {
            salvando = false;
            FinalizarAtendimentoButton.IsEnabled = !registrado && ImpedimentoAtendimentoApi() == null;
        }
        if (registrado)
        {
            await DisplayAlertAsync("Atendimento registrado",
                "A consulta foi marcada como realizada e o prontuário foi salvo no banco.", "OK");
            await Shell.Current.GoToAsync("..");
        }
    }

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

