using System.Collections.ObjectModel;

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
    // VERIFICA DIA ÚTIL
    // =============================================

    private bool EhDiaUtil(
        DateTime data)
    {
        return
            data.DayOfWeek !=
                DayOfWeek.Saturday &&

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


        while (!EhDiaUtil(resultado))
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


        // FIM DE SEMANA NÃO POSSUI AGENDA

        if (!EhDiaUtil(data))
        {
            AgendaCollectionView.ItemsSource =
                Agenda;


            return;
        }


        List<ConsultaMedico>
            consultasDoDia =
                ConsultasMedicoDados
                    .ObterConsultasPorData(
                        data);


        TimeSpan[] horariosAgenda =
        {
            new TimeSpan(8, 0, 0),
            new TimeSpan(9, 0, 0),
            new TimeSpan(10, 0, 0),
            new TimeSpan(11, 0, 0),

            new TimeSpan(13, 0, 0),
            new TimeSpan(14, 0, 0),
            new TimeSpan(15, 0, 0),
            new TimeSpan(16, 0, 0)
        };


        foreach (
            TimeSpan horario
            in horariosAgenda)
        {
            ConsultaMedico? consulta =
                consultasDoDia
                    .FirstOrDefault(c =>
                        c.Horario ==
                        horario);


            // EXISTE CONSULTA

            if (consulta != null)
            {
                Agenda.Add(
                    new ConsultaAgenda
                    {
                        IdConsulta =
                            consulta.Id,

                        Horario =
                            consulta.HorarioTexto,

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


            // VERIFICA BLOQUEIO

            BloqueioHorario? bloqueio =
                EncontrarBloqueio(
                    data,
                    horario);


            if (bloqueio != null)
            {
                Agenda.Add(
                    new ConsultaAgenda
                    {
                        IdConsulta =
                            null,

                        Horario =
                            horario.ToString(
                                @"hh\:mm"),

                        Paciente =
                            "-----------",

                        Status =
                            "Horário bloqueado",

                        StatusCor =
                            Colors.Red
                    });


                continue;
            }


            // HORÁRIO LIVRE

            Agenda.Add(
                new ConsultaAgenda
                {
                    IdConsulta =
                        null,

                    Horario =
                        horario.ToString(
                            @"hh\:mm"),

                    Paciente =
                        "-----------",

                    Status =
                        "Horário disponível",

                    StatusCor =
                        Colors.Blue
                });
        }


        AgendaCollectionView.ItemsSource =
            Agenda;
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

            "Horário bloqueado" =>
                Colors.Red,

            _ =>
                Colors.Black
        };
    }


    // =============================================
    // LOCALIZA BLOQUEIOS
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
                    bloqueio.DataInicial.Date &&

                data.Date <=
                    bloqueio.DataFinal.Date;


            if (!dataDentroDoBloqueio)
                continue;


            bool horarioDentroDoBloqueio =
                horario >=
                    bloqueio.HorarioInicial &&

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
    } = Colors.Black;
}