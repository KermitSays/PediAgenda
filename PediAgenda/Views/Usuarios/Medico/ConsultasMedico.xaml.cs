using System.Collections.ObjectModel;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class ConsultasMedico : ContentPage
{
    public ObservableCollection<ConsultaAgenda>
        Agenda
    {
        get;
        set;
    } = new();


    private bool ajustandoData;


    // =============================================
    // MÉDICO LOCAL TEMPORÁRIO
    // =============================================
    //
    // Enquanto a API ainda não devolve o id_medico
    // associado ao usuário logado, usamos o médico
    // local de ID 1.
    //
    // IMPORTANTE:
    // id_usuario e id_medico NÃO são a mesma coisa.
    //
    // Quando a API de médicos/agenda estiver pronta,
    // este valor será substituído pelo id_medico real.

    private const int IdMedicoAgendaLocal =
        1;


    public ConsultasMedico()
    {
        InitializeComponent();


        DateTime primeiraDataDisponivel =
            ProximoDiaUtil(
                DateTime.Today);


        DataConsultaPicker.MinimumDate =
            primeiraDataDisponivel;


        DataConsultaPicker.Date =
            primeiraDataDisponivel;


        DataConsultaPicker.DateSelected +=
            DataConsultaPicker_DateSelected;


        AgendaCollectionView.ItemsSource =
            Agenda;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        DateTime dataSelecionada =
            DataConsultaPicker.Date
            ?? ProximoDiaUtil(
                DateTime.Today);


        CarregarAgenda(
            dataSelecionada);
    }


    // =============================================
    // DIA ÚTIL
    // =============================================

    private bool EhDiaUtil(
        DateTime data)
    {
        return
            data.DayOfWeek !=
                DayOfWeek.Saturday

            &&

            data.DayOfWeek !=
                DayOfWeek.Sunday;
    }


    // =============================================
    // PRÓXIMO DIA ÚTIL
    // =============================================

    private DateTime ProximoDiaUtil(
        DateTime data)
    {
        DateTime resultado =
            data.Date;


        while (!EhDiaUtil(
            resultado))
        {
            resultado =
                resultado.AddDays(1);
        }


        return resultado;
    }


    // =============================================
    // ALTERAÇÃO DA DATA
    // =============================================

    private async void DataConsultaPicker_DateSelected(
        object? sender,
        DateChangedEventArgs e)
    {
        if (ajustandoData)
            return;


        DateTime dataSelecionada =
            e.NewDate
            ?? DateTime.Today;


        if (!EhDiaUtil(
            dataSelecionada))
        {
            await DisplayAlertAsync(
                "Data indisponível",
                "A clínica não possui atendimento aos sábados e domingos.",
                "OK");


            ajustandoData =
                true;


            DataConsultaPicker.Date =
                ProximoDiaUtil(
                    dataSelecionada);


            ajustandoData =
                false;


            CarregarAgenda(
                DataConsultaPicker.Date
                ?? ProximoDiaUtil(
                    DateTime.Today));


            return;
        }


        CarregarAgenda(
            dataSelecionada);
    }


    // =============================================
    // CARREGA A AGENDA
    // =============================================

    private void CarregarAgenda(
        DateTime data)
    {
        Agenda.Clear();


        if (!EhDiaUtil(data))
        {
            return;
        }


        // CONSULTAS EXISTENTES DESTA DATA

        List<ConsultaMedico>
            consultasDoDia =
                ConsultasMedicoDados
                    .ObterConsultasPorData(
                        data);


        // =========================================
        // HORÁRIOS DA FONTE COMPARTILHADA
        // =========================================
        //
        // Não existe mais uma grade fixa própria
        // dentro da tela do Médico.

        List<HorarioAgendamentoItem>
            horariosDoDia =
                HorarioAgendamentoDados
                    .ObterHorariosDoDia(
                        IdMedicoAgendaLocal,
                        data);


        foreach (
            HorarioAgendamentoItem horario
            in horariosDoDia)
        {
            ConsultaMedico? consulta =
                consultasDoDia
                    .FirstOrDefault(c =>
                        c.Horario ==
                            horario.HoraInicio);


            // =====================================
            // CONSULTA
            // =====================================

            if (consulta != null)
            {
                Agenda.Add(
                    new ConsultaAgenda
                    {
                        IdConsulta =
                            consulta.Id,

                        IdPaciente =
                            consulta.IdPaciente,

                        Horario =
                            horario.HoraTexto,

                        Paciente =
                            consulta.Paciente,

                        Status =
                            consulta.Status,

                        StatusCor =
                            ObterCorStatus(
                                consulta.Status)
                    });


                continue;
            }


            // =====================================
            // BLOQUEIO DO MÉDICO
            // =====================================

            BloqueioHorario? bloqueio =
                EncontrarBloqueio(
                    data,
                    horario.HoraInicio);


            if (bloqueio != null)
            {
                Agenda.Add(
                    new ConsultaAgenda
                    {
                        IdConsulta =
                            null,

                        IdPaciente =
                            null,

                        Horario =
                            horario.HoraTexto,

                        Paciente =
                            "-----------",

                        Status =
                            "Horário bloqueado",

                        StatusCor =
                            Colors.Red
                    });


                continue;
            }


            // =====================================
            // HORÁRIO OCUPADO NA FONTE COMPARTILHADA
            // =====================================
            //
            // Por exemplo: um horário que foi reservado
            // pelo fluxo do Responsável.

            if (!horario.Disponivel)
            {
                Agenda.Add(
                    new ConsultaAgenda
                    {
                        IdConsulta =
                            null,

                        IdPaciente =
                            null,

                        Horario =
                            horario.HoraTexto,

                        Paciente =
                            "-----------",

                        Status =
                            "Horário ocupado",

                        StatusCor =
                            Colors.Gray
                    });


                continue;
            }


            // =====================================
            // HORÁRIO DISPONÍVEL
            // =====================================

            Agenda.Add(
                new ConsultaAgenda
                {
                    IdConsulta =
                        null,

                    IdPaciente =
                        null,

                    Horario =
                        horario.HoraTexto,

                    Paciente =
                        "-----------",

                    Status =
                        "Horário disponível",

                    StatusCor =
                        Colors.Blue
                });
        }
    }


    // =============================================
    // CONSULTA SELECIONADA
    // =============================================

    private async void AgendaCollectionView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (
            e.CurrentSelection.FirstOrDefault()
            is not ConsultaAgenda item)
        {
            return;
        }


        AgendaCollectionView.SelectedItem =
            null;


        // DISPONÍVEL, OCUPADO OU BLOQUEADO

        if (!item.IdConsulta.HasValue)
        {
            return;
        }


        ConsultaMedico? consulta =
            ConsultasMedicoDados
                .ObterConsultaPorId(
                    item.IdConsulta.Value);


        if (consulta == null)
        {
            await DisplayAlertAsync(
                "Consulta não encontrada",
                "Não foi possível localizar os dados desta consulta.",
                "OK");


            return;
        }


        // CANCELADA

        if (
            consulta.Status.Equals(
                "Cancelado",
                StringComparison.OrdinalIgnoreCase)

            ||

            consulta.Status.Equals(
                "Cancelada",
                StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Consulta cancelada",
                "Não é possível registrar atendimento em uma consulta cancelada.",
                "OK");


            return;
        }


        // REALIZADA

        if (
            consulta.Status.Equals(
                "Realizada",
                StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Atendimento já realizado",
                "Esta consulta já possui um atendimento registrado.",
                "OK");


            return;
        }


        // CONSULTA FUTURA

        if (
            consulta.Data.Date >
            DateTime.Today)
        {
            await DisplayAlertAsync(
                "Atendimento ainda indisponível",
                "O atendimento só poderá ser registrado no dia da consulta.",
                "OK");


            return;
        }


        // PACIENTE NÃO VINCULADO

        if (!consulta.IdPaciente.HasValue)
        {
            await DisplayAlertAsync(
                "Paciente não vinculado",
                "Esta consulta ainda não está vinculada a um paciente cadastrado.",
                "OK");


            return;
        }


        // REGISTRAR ATENDIMENTO

        await Shell.Current.GoToAsync(
            nameof(RegistrarAtendimento),

            new Dictionary<string, object>
            {
                {
                    "IdConsulta",
                    consulta.Id.ToString()
                }
            });
    }


    // =============================================
    // COR DO STATUS
    // =============================================

    private Color ObterCorStatus(
        string status)
    {
        return status switch
        {
            "Confirmado" =>
                Colors.Green,

            "Confirmada" =>
                Colors.Green,

            "Cancelado" =>
                Colors.Red,

            "Cancelada" =>
                Colors.Red,

            "Por Confirmar" =>
                Colors.Orange,

            "Por confirmar" =>
                Colors.Orange,

            "Realizada" =>
                Colors.Green,

            "Horário disponível" =>
                Colors.Blue,

            "Horário ocupado" =>
                Colors.Gray,

            "Horário bloqueado" =>
                Colors.Red,

            _ =>
                Colors.Black
        };
    }


    // =============================================
    // BLOQUEIOS
    // =============================================

    private BloqueioHorario? EncontrarBloqueio(
        DateTime data,
        TimeSpan horario)
    {
        foreach (
            BloqueioHorario bloqueio
            in BloqueiosMedico.Bloqueios)
        {
            bool dataDentroDoBloqueio =
                data.Date >=
                    bloqueio.DataInicial.Date

                &&

                data.Date <=
                    bloqueio.DataFinal.Date;


            if (!dataDentroDoBloqueio)
                continue;


            bool horarioDentroDoBloqueio =
                horario >=
                    bloqueio.HorarioInicial

                &&

                horario <
                    bloqueio.HorarioFinal;


            if (horarioDentroDoBloqueio)
            {
                return bloqueio;
            }
        }


        return null;
    }


    // =============================================
    // BLOQUEAR HORÁRIO
    // =============================================

    private async void BloquearHorarioButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(BloquearHorario));
    }


    // =============================================
    // LIBERAR HORÁRIOS
    // =============================================

    private async void LiberarHorariosButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(LiberarHorarios));
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


// =============================================
// ITEM VISUAL DA AGENDA
// =============================================

public class ConsultaAgenda
{
    public Guid? IdConsulta
    {
        get;
        set;
    }


    public int? IdPaciente
    {
        get;
        set;
    }


    public string Horario
    {
        get;
        set;
    } = string.Empty;


    public string Paciente
    {
        get;
        set;
    } = string.Empty;


    public string Status
    {
        get;
        set;
    } = string.Empty;


    public Color StatusCor
    {
        get;
        set;
    } =
        Colors.Black;
}