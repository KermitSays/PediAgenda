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

        BindingContext = this;
    }
}