using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class PacientesRecepcao : ContentPage
{
    private List<PacienteRecepcaoItem> _todosPacientes;

    public ObservableCollection<PacienteRecepcaoItem> PacientesFiltrados { get; set; }

    public PacientesRecepcao()
    {
        InitializeComponent();

        _todosPacientes = PacientesRecepcaoDados.Pacientes;

        PacientesFiltrados =
            new ObservableCollection<PacienteRecepcaoItem>(_todosPacientes);

        BindingContext = this;
    }


    // FILTRO DE BUSCA
    private void PacienteSearchBar_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        string texto =
            e.NewTextValue?.Trim().ToLower() ?? string.Empty;

        var filtrados =
            string.IsNullOrWhiteSpace(texto)
            ? _todosPacientes
            : _todosPacientes
                .Where(p =>
                    p.Nome.ToLower().Contains(texto) ||
                    p.Responsavel.ToLower().Contains(texto))
                .ToList();

        PacientesFiltrados.Clear();

        foreach (var paciente in filtrados)
        {
            PacientesFiltrados.Add(paciente);
        }
    }


    // ACESSAR PACIENTE
    private async void DetalhesButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is Button botao &&
            botao.CommandParameter is PacienteRecepcaoItem paciente)
        {
            await Shell.Current.GoToAsync(
                $"{nameof(DetalhesPacienteRecepcao)}?PacienteId={paciente.Id}");
        }
    }
}