using System.Collections.ObjectModel;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class LiberarHorarios : ContentPage
{
    public ObservableCollection<BloqueioExibicao>
        Bloqueios
    {
        get;
        set;
    }


    public LiberarHorarios()
    {
        InitializeComponent();


        CarregarBloqueios();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        CarregarBloqueios();
    }


    // =============================================
    // CARREGA BLOQUEIOS
    // =============================================

    private void CarregarBloqueios()
    {
        Bloqueios =
            new ObservableCollection<BloqueioExibicao>();


        foreach (
            BloqueioHorario bloqueio
            in BloqueiosMedico.Bloqueios)
        {
            Bloqueios.Add(
                new BloqueioExibicao
                {
                    Id =
                        bloqueio.Id,

                    PeriodoData =
                        bloqueio.DataInicial.Date ==
                        bloqueio.DataFinal.Date

                            ? bloqueio.DataInicial
                                .ToString(
                                    "dd/MM/yyyy")

                            : $"{bloqueio.DataInicial:dd/MM/yyyy} " +
                              $"até " +
                              $"{bloqueio.DataFinal:dd/MM/yyyy}",


                    PeriodoHorario =
                        $"{bloqueio.HorarioInicial:hh\\:mm} às " +
                        $"{bloqueio.HorarioFinal:hh\\:mm}",


                    MotivoTexto =
                        $"Motivo: {bloqueio.Motivo}"
                });
        }


        BloqueiosCollectionView.ItemsSource =
            Bloqueios;
    }


    // =============================================
    // EDITAR
    // =============================================

    private async void EditarBloqueioButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            sender is Button button

            &&

            button.BindingContext
                is BloqueioExibicao bloqueio)
        {
            await Shell.Current.GoToAsync(
                nameof(EditarBloqueio),

                new Dictionary<string, object>
                {
                    {
                        "IdBloqueio",
                        bloqueio.Id.ToString()
                    }
                });
        }
    }


    // =============================================
    // LIBERAR
    // =============================================

    private async void LiberarPeriodoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            sender is not Button button

            ||

            button.BindingContext
                is not BloqueioExibicao bloqueioExibicao)
        {
            return;
        }


        BloqueioHorario? bloqueio =
            BloqueiosMedico.Bloqueios
                .FirstOrDefault(item =>
                    item.Id ==
                    bloqueioExibicao.Id);


        if (bloqueio == null)
        {
            await DisplayAlertAsync(
                "Atenção",
                "Não foi possível localizar o bloqueio.",
                "OK");


            return;
        }


        bool confirmar =
            await DisplayAlertAsync(
                "Liberar período",

                $"Deseja liberar todo este período?\n\n" +
                $"Data: {bloqueioExibicao.PeriodoData}\n" +
                $"Horário: {bloqueioExibicao.PeriodoHorario}\n" +
                $"{bloqueioExibicao.MotivoTexto}",

                "Liberar",
                "Cancelar");


        if (!confirmar)
            return;


        // PRIMEIRO ATUALIZA A FONTE DE HORÁRIOS.

        HorarioAgendamentoDados
            .LiberarBloqueio(
                bloqueio.Id);


        // DEPOIS REMOVE O BLOQUEIO DA LISTA.

        BloqueiosMedico.Bloqueios.Remove(
            bloqueio);


        await DisplayAlertAsync(
            "Período liberado",
            "Todo o período foi liberado com sucesso.",
            "OK");


        CarregarBloqueios();
    }


    // =============================================
    // ITEM DE EXIBIÇÃO
    // =============================================

    public class BloqueioExibicao
    {
        public Guid Id
        {
            get;
            set;
        }


        public string PeriodoData
        {
            get;
            set;
        } =
            string.Empty;


        public string PeriodoHorario
        {
            get;
            set;
        } =
            string.Empty;


        public string MotivoTexto
        {
            get;
            set;
        } =
            string.Empty;
    }
}