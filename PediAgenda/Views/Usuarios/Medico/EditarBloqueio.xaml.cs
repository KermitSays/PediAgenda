using PediAgenda.Dados;
using PediAgenda.Views.Usuarios.Recepcao;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(
    nameof(IdBloqueio),
    "IdBloqueio")]
public partial class EditarBloqueio : ContentPage
{
    private string idBloqueio =
        string.Empty;


    private BloqueioHorario?
        bloqueioAtual;


    public string IdBloqueio
    {
        get =>
            idBloqueio;

        set
        {
            idBloqueio =
                value;


            if (
                DataInicialPicker !=
                null)
            {
                CarregarBloqueio();
            }
        }
    }


    public EditarBloqueio()
    {
        InitializeComponent();
    }


    private void CarregarBloqueio()
    {
        if (
            !Guid.TryParse(
                IdBloqueio,
                out Guid id))
        {
            return;
        }


        bloqueioAtual =
            BloqueiosMedico.Bloqueios
                .FirstOrDefault(b =>
                    b.Id == id);


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


    private List<ConsultaMedico>
        EncontrarConsultasMedicoAfetadas(
            DateTime dataInicial,
            DateTime dataFinal,
            TimeSpan horarioInicial,
            TimeSpan horarioFinal)
    {
        if (
            bloqueioAtual == null

            ||

            bloqueioAtual.IdMedico !=
                1)
        {
            return
                new List<ConsultaMedico>();
        }


        return ConsultasMedicoDados.Consultas
            .Where(consulta =>
                consulta.Data.Date >=
                    dataInicial.Date

                &&

                consulta.Data.Date <=
                    dataFinal.Date

                &&

                consulta.Horario >=
                    horarioInicial

                &&

                consulta.Horario <
                    horarioFinal

                &&

                StatusConsulta.EhAtiva(
                    consulta.Status))
            .ToList();
    }


    private List<(
        PacienteRecepcaoItem Paciente,
        ConsultaPacienteRecepcaoItem Consulta)>
        EncontrarConsultasRecepcaoAfetadas(
            DateTime dataInicial,
            DateTime dataFinal,
            TimeSpan horarioInicial,
            TimeSpan horarioFinal)
    {
        List<(
            PacienteRecepcaoItem Paciente,
            ConsultaPacienteRecepcaoItem Consulta)>
            resultado =
                new();


        if (bloqueioAtual == null)
            return resultado;


        foreach (
            PacienteRecepcaoItem paciente
            in PacientesRecepcaoDados.Pacientes)
        {
            foreach (
                ConsultaPacienteRecepcaoItem consulta
                in paciente.Consultas)
            {
                if (
                    !consulta.HorarioId
                        .HasValue)
                {
                    continue;
                }


                int idMedico =
                    AgendaMedicaRecepcaoDados
                        .ObterIdMedico(
                            consulta.Medico);


                if (
                    idMedico !=
                    bloqueioAtual.IdMedico)
                {
                    continue;
                }


                bool dentro =
                    consulta.Data.Date >=
                        dataInicial.Date

                    &&

                    consulta.Data.Date <=
                        dataFinal.Date

                    &&

                    consulta.Horario >=
                        horarioInicial

                    &&

                    consulta.Horario <
                        horarioFinal;


                if (
                    dentro

                    &&

                    StatusConsulta.EhAtiva(
                        consulta.Status))
                {
                    resultado.Add(
                        (
                            paciente,
                            consulta
                        ));
                }
            }
        }


        return resultado;
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
            DataInicialPicker.Date
            ?? DateTime.Today;


        DateTime dataFinal =
            DataFinalPicker.Date
            ?? DateTime.Today;


        TimeSpan horarioInicial =
            HorarioInicialPicker.Time
            ?? bloqueioAtual.HorarioInicial;


        TimeSpan horarioFinal =
            HorarioFinalPicker.Time
            ?? bloqueioAtual.HorarioFinal;


        string motivo =
            MotivoEditor.Text?
                .Trim()
            ?? string.Empty;


        if (
            dataFinal <
            dataInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "A Data Final não pode ser anterior à Data Inicial.",
                "OK");


            return;
        }


        if (
            horarioFinal <=
            horarioInicial)
        {
            await DisplayAlertAsync(
                "Atenção",
                "O Horário Final deve ser posterior ao Horário Inicial.",
                "OK");


            return;
        }


        if (
            string.IsNullOrWhiteSpace(
                motivo))
        {
            await DisplayAlertAsync(
                "Atenção",
                "Informe o motivo do bloqueio.",
                "OK");


            return;
        }


        List<ConsultaMedico>
            consultasMedico =
                EncontrarConsultasMedicoAfetadas(
                    dataInicial,
                    dataFinal,
                    horarioInicial,
                    horarioFinal);


        List<(
            PacienteRecepcaoItem Paciente,
            ConsultaPacienteRecepcaoItem Consulta)>
            consultasRecepcao =
                EncontrarConsultasRecepcaoAfetadas(
                    dataInicial,
                    dataFinal,
                    horarioInicial,
                    horarioFinal);


        int total =
            consultasMedico.Count +
            consultasRecepcao.Count;


        string mensagem =
            "Deseja salvar as alterações deste bloqueio?";


        if (total > 0)
        {
            mensagem +=
                $"\n\nAtenção: {total} " +
                $"{(
                    total == 1
                        ? "consulta será afetada."
                        : "consultas serão afetadas."
                )}";


            mensagem +=
                "\n\nAs consultas afetadas serão canceladas.";
        }


        bool confirmar =
            await DisplayAlertAsync(
                "Salvar alteração",
                mensagem,
                "Salvar",
                "Cancelar");


        if (!confirmar)
            return;


        int idMedico =
            bloqueioAtual.IdMedico;


        HorarioAgendamentoDados
            .LiberarBloqueio(
                bloqueioAtual.Id);


        bloqueioAtual.DataInicial =
            dataInicial;


        bloqueioAtual.DataFinal =
            dataFinal;


        bloqueioAtual.HorarioInicial =
            horarioInicial;


        bloqueioAtual.HorarioFinal =
            horarioFinal;


        bloqueioAtual.Motivo =
            motivo;


        HorarioAgendamentoDados
            .BloquearPeriodo(
                bloqueioAtual.Id,
                idMedico,
                dataInicial,
                dataFinal,
                horarioInicial,
                horarioFinal);


        foreach (
            ConsultaMedico consulta
            in consultasMedico)
        {
            consulta.Status =
                StatusConsulta.Cancelada;


            consulta.CanceladaPorBloqueio =
                true;


            consulta.MotivoCancelamento =
                motivo;


            HorarioAgendamentoItem?
                horario =
                    HorarioAgendamentoDados
                        .ObterPorDataHora(
                            idMedico,
                            consulta.Data,
                            consulta.Horario);


            if (horario != null)
            {
                HorarioAgendamentoDados
                    .LiberarHorario(
                        horario.IdHorario);
            }


            NotificacoesDados.Notificacoes.Add(
                new Notificacao
                {
                    Titulo =
                        "Consulta cancelada",

                    Mensagem =
                        $"A consulta de {consulta.Paciente} " +
                        $"do dia {consulta.Data:dd/MM/yyyy} às " +
                        $"{consulta.Horario:hh\\:mm} " +
                        $"foi cancelada após uma alteração " +
                        $"no bloqueio da agenda." +
                        $"\n\nMotivo: {motivo}",

                    DataHora =
                        DateTime.Now,

                    Lida =
                        false
                });
        }


        foreach (
            var item
            in consultasRecepcao)
        {
            ConsultaPacienteRecepcaoItem consulta =
                item.Consulta;


            if (
                !consulta.HorarioId
                    .HasValue)
            {
                continue;
            }


            int idHorario =
                consulta.HorarioId.Value;


            consulta.Status =
                StatusConsulta.Cancelada;


            consulta.HorarioId =
                null;


            HorarioAgendamentoDados
                .LiberarHorario(
                    idHorario);
        }


        await DisplayAlertAsync(
            "Alteração salva",

            total > 0
                ? "O bloqueio foi atualizado e as consultas afetadas foram canceladas."
                : "O bloqueio foi atualizado com sucesso.",

            "OK");


        await Shell.Current.GoToAsync(
            "..");
    }


    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            "..");
    }
}