using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class Notificacoes : ContentPage
{
    public ObservableCollection<Notificacao>
        NotificacoesLista
    { get; set; }


    public Notificacoes()
    {
        InitializeComponent();


        NotificacoesLista =
            new ObservableCollection<Notificacao>();


        BindingContext =
            this;


        CarregarNotificacoes();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        CarregarNotificacoes();
    }


    private void CarregarNotificacoes()
    {
        NotificacoesLista.Clear();


        var notificacoesOrdenadas =
            NotificacoesDados.Notificacoes
                .OrderByDescending(n =>
                    n.DataHora);


        foreach (Notificacao notificacao
            in notificacoesOrdenadas)
        {
            NotificacoesLista.Add(
                notificacao);
        }
    }
}