using System.Collections.ObjectModel;
using PediAgenda.Views.Usuarios.Responsavel;

namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(
    nameof(IdMedico),
    "IdMedico")]
public partial class LiberarHorarios : ContentPage
{
    public ObservableCollection<BloqueioExibicao>
        Bloqueios
    {
        get;
        set;
    } =
        new();


    private int idMedicoAgenda =
        1;


    private string idMedico =
        "1";


    public string IdMedico
    {
        get =>
            idMedico;

        set
        {
            idMedico =
                value;


            if (
                int.TryParse(
                    value,
                    out int id)

                &&

                id > 0)
            {
                idMedicoAgenda =
                    id;
            }


            if (
                BloqueiosCollectionView !=
                null)
            {
                CarregarBloqueios();
            }
        }
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


    private void CarregarBloqueios()
    {
        Bloqueios.Clear();


        foreach (
            BloqueioHorario bloqueio
            in BloqueiosMedico.Bloqueios
                .Where(b =>
                    b.IdMedico ==
                    idMedicoAgenda))
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

                            : $"{bloqueio.DataInicial:dd/MM/yyyy} até " +
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


    private async void LiberarPeriodoButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (
            sender is not Button button

            ||

            button.BindingContext
                is not BloqueioExibicao item)
        {
            return;
        }


        BloqueioHorario? bloqueio =
            BloqueiosMedico.Bloqueios
                .FirstOrDefault(b =>
                    b.Id ==
                    item.Id);


        if (bloqueio == null)
            return;


        bool confirmar =
            await DisplayAlertAsync(
                "Liberar período",

                $"Deseja liberar todo este período?\n\n" +
                $"Data: {item.PeriodoData}\n" +
                $"Horário: {item.PeriodoHorario}\n" +
                $"{item.MotivoTexto}",

                "Liberar",
                "Cancelar");


        if (!confirmar)
            return;


        HorarioAgendamentoDados
            .LiberarBloqueio(
                bloqueio.Id);


        BloqueiosMedico.Bloqueios.Remove(
            bloqueio);


        CarregarBloqueios();


        await DisplayAlertAsync(
            "Período liberado",
            "Todo o período foi liberado com sucesso.",
            "OK");
    }


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