using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class PacientesMedico : ContentPage
{
    public ObservableCollection<PacienteMedicoItem>
        Pacientes
    { get; set; }


    public ObservableCollection<PacienteMedicoItem>
        PacientesFiltrados
    { get; set; }


    public PacientesMedico()
    {
        InitializeComponent();


        Pacientes =
            MedicoDados.Pacientes;


        PacientesFiltrados =
            new ObservableCollection<PacienteMedicoItem>(
                Pacientes);


        BindingContext =
            this;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        AtualizarLista();
    }


    // ATUALIZA A LISTA

    private void AtualizarLista()
    {
        string textoBusca =
            BuscarPacienteEntry?.Text?
                .Trim()
            ?? string.Empty;


        FiltrarPacientes(
            textoBusca);
    }


    // PESQUISA PACIENTES PELO NOME

    private void BuscarPacienteEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        string textoBusca =
            e.NewTextValue?.Trim()
            ?? string.Empty;


        FiltrarPacientes(
            textoBusca);
    }


    private void FiltrarPacientes(
        string textoBusca)
    {
        PacientesFiltrados.Clear();


        IEnumerable<PacienteMedicoItem>
            pacientesFiltrados =
                Pacientes;


        if (!string.IsNullOrWhiteSpace(
            textoBusca))
        {
            pacientesFiltrados =
                pacientesFiltrados
                    .Where(p =>
                        p.Nome.Contains(
                            textoBusca,
                            StringComparison.OrdinalIgnoreCase));
        }


        foreach (
            PacienteMedicoItem paciente
            in pacientesFiltrados
                .OrderBy(p =>
                    p.Nome))
        {
            PacientesFiltrados.Add(
                paciente);
        }
    }


    // ABRE O PRONTUÁRIO DO PACIENTE

    private async void PacientesMedico_Clicked(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not Border border)
            return;


        if (border.BindingContext
            is not PacienteMedicoItem paciente)
        {
            return;
        }


        await Shell.Current.GoToAsync(
            nameof(ProntuarioPaciente),
            new Dictionary<string, object>
            {
                {
                    "IdPaciente",
                    paciente.IdPaciente
                }
            });
    }
}