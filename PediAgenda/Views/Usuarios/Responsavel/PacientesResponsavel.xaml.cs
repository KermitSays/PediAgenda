using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class PacientesResponsavel : ContentPage
{
    public ObservableCollection<PacienteResponsavelItem>
        PacientesLista
    { get; set; }


    public PacientesResponsavel()
    {
        InitializeComponent();


        PacientesLista =
            ResponsavelDados.Pacientes;


        BindingContext =
            this;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        // Recarrega os cards para atualizar
        // automaticamente o histórico.

        PacientesCollectionView.ItemsSource =
            null;


        PacientesCollectionView.ItemsSource =
            PacientesLista;
    }


    // ABRE / FECHA O HISTÓRICO DO PACIENTE

    private void PacienteCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is BindableObject elemento &&
            elemento.BindingContext
                is PacienteResponsavelItem paciente)
        {
            paciente.IsHistoricoExpanded =
                !paciente.IsHistoricoExpanded;
        }
    }


    // Historico somente leitura, sem tela de detalhes.

    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}