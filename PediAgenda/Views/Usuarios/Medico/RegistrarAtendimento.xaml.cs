using System.Globalization;
using System.Text;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class RegistrarAtendimento : ContentPage, IQueryAttributable
{
    private AtendimentoMedicoApi? atendimento;
    private long sessao;
    private bool salvando;
    private bool registrado;
    private bool carregado;
    private CancellationTokenSource? carregamento;
    private static readonly TimeZoneInfo fusoClinica = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    public RegistrarAtendimento() { InitializeComponent(); }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        atendimento = query.TryGetValue("AtendimentoApi", out var valor) ? valor as AtendimentoMedicoApi : null;
        sessao = SessaoUsuario.Versao;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    protected override void OnDisappearing()
    {
        carregamento?.Cancel();
        base.OnDisappearing();
    }

    private async void AtualizarButton_Clicked(object? sender, EventArgs e) => await CarregarAsync();

    private string? Impedimento()
    {
        if (sessao != SessaoUsuario.Versao || atendimento?.Horario.Consulta is not { } consulta)
            return "Abra a consulta novamente pela agenda.";
        if (!carregado) return "Atualize os dados da consulta antes de finalizar.";
        if (registrado) return "Esta consulta já possui um prontuário.";
        if (consulta.Status == "CANCELADA") return "Não é possível atender uma consulta cancelada.";
        if (!TimeOnly.TryParseExact(atendimento.Horario.Inicio, "HH:mm", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var hora)) return "O horário da consulta é inválido.";
        var agora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, fusoClinica).DateTime;
        if (atendimento.Data.Date.Add(hora.ToTimeSpan()) > agora)
            return "A consulta ainda não começou.";
        return null;
    }

    private async Task CarregarAsync()
    {
        if (salvando) return;
        carregamento?.Cancel();
        using var atual = new CancellationTokenSource();
        carregamento = atual;
        carregado = false;
        FinalizarAtendimentoButton.IsEnabled = false;
        EstadoLabel.Text = "Carregando consulta…";
        try
        {
            if (sessao != SessaoUsuario.Versao || atendimento?.Horario.Consulta is not { } selecionada)
                throw new InvalidOperationException("Abra uma consulta pela agenda do médico conectado.");
            var horarios = await MedicoApi.ObterAgendaAsync(atendimento.Data, atual.Token);
            var horario = horarios.FirstOrDefault(h => h.Consulta?.Id == selecionada.Id)
                ?? throw new InvalidOperationException("A consulta não está mais disponível. Volte à agenda.");
            var prontuario = await MedicoApi.BuscarProntuarioAsync(selecionada.Id, atual.Token);
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            atendimento = atendimento with { Horario = horario };
            var consulta = horario.Consulta!;
            PacienteLabel.Text = consulta.Paciente.Nome;
            ResponsavelLabel.Text = consulta.Responsavel.Nome;
            DataLabel.Text = atendimento.Data.ToString("dd/MM/yyyy");
            HorarioLabel.Text = horario.Inicio;
            TipoConsultaLabel.Text = consulta.TipoAtendimento;
            registrado = prontuario != null;
            carregado = true;
            EstadoLabel.Text = Impedimento() ?? "Consulta disponível para registrar o atendimento.";
            ObservacoesEditor.IsEnabled = !registrado;
            FinalizarAtendimentoButton.IsEnabled = Impedimento() == null;
        }
        catch (Exception ex)
        {
            if (atual.IsCancellationRequested || sessao != SessaoUsuario.Versao) return;
            EstadoLabel.Text = "Não foi possível carregar. Toque em Atualizar consulta.";
            await DisplayAlertAsync("Atendimento", ex is InvalidOperationException
                ? ex.Message : "Falha ao carregar a consulta. Tente novamente.", "OK");
        }
        finally
        {
            if (ReferenceEquals(carregamento, atual)) carregamento = null;
        }
    }

    private async void FinalizarAtendimentoButton_Clicked(object? sender, EventArgs e)
    {
        if (salvando) return;
        if (Impedimento() is { } impedimento)
        {
            await DisplayAlertAsync("Atendimento indisponível", impedimento, "OK");
            return;
        }
        string observacoes = ObservacoesEditor.Text?.Trim() ?? "";
        if (observacoes.Length == 0 || Encoding.UTF8.GetByteCount(observacoes) > 65535)
        {
            await DisplayAlertAsync("Observações", "Preencha as observações com até 65.535 bytes de texto UTF-8.", "OK");
            return;
        }
        salvando = true;
        FinalizarAtendimentoButton.IsEnabled = AtualizarButton.IsEnabled = false;
        try
        {
            bool confirmar = await DisplayAlertAsync("Finalizar atendimento",
                $"Deseja finalizar o atendimento de {atendimento!.Horario.Consulta!.Paciente.Nome}?\nA consulta será marcada como realizada e o prontuário será salvo.",
                "FINALIZAR", "VOLTAR");
            if (!confirmar) return;
            if (sessao != SessaoUsuario.Versao) return;
            await MedicoApi.RegistrarAtendimentoAsync(atendimento.Horario.Consulta.Id, observacoes);
            if (sessao != SessaoUsuario.Versao) return;
            registrado = true;
            ObservacoesEditor.IsEnabled = false;
            await DisplayAlertAsync("Atendimento registrado", "A consulta foi marcada como realizada e o prontuário foi salvo no banco.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            if (sessao != SessaoUsuario.Versao) return;
            // O servidor pode ter confirmado antes de a conexão cair. Exige releitura
            // para não incentivar o reenvio de um registro já persistido.
            carregado = false;
            EstadoLabel.Text = "Atualize a consulta para conferir o registro antes de tentar novamente.";
            await DisplayAlertAsync("Não foi possível confirmar o registro",
                (ex is InvalidOperationException ? ex.Message : "Verifique a conexão.")
                + "\nSuas observações foram mantidas. Atualize a consulta antes de repetir o envio.", "OK");
        }
        finally
        {
            salvando = false;
            AtualizarButton.IsEnabled = true;
            FinalizarAtendimentoButton.IsEnabled = Impedimento() == null;
        }
    }

    private async void VoltarButton_Clicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
