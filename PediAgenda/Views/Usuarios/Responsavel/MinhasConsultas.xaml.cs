using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class MinhasConsultas : ContentPage
{
    private ObservableCollection<PacienteResponsavelItem>
        pacientes;


    public MinhasConsultas()
    {
        InitializeComponent();


        pacientes =
            ResponsavelDados.Pacientes;


        PacientesCollectionView.ItemsSource =
            pacientes;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        // Recria os cards para que os filtros
        // e a ordenação sejam recalculados.

        PacientesCollectionView.ItemsSource =
            null;


        PacientesCollectionView.ItemsSource =
            pacientes;
    }


    // ABRE / FECHA O PACIENTE

    private void PacienteCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is BindableObject elemento &&
            elemento.BindingContext
                is PacienteResponsavelItem paciente)
        {
            paciente.IsExpanded =
                !paciente.IsExpanded;
        }
    }


    // Abre diretamente a tela de cancelamento, sem detalhes intermediarios.
    private async void ConsultaCard_Tapped(object sender, TappedEventArgs e)
    {
        if (sender is not BindableObject elemento ||
            elemento.BindingContext is not ConsultaResponsavelItem consulta)
            return;

        PacienteResponsavelItem? paciente = pacientes.FirstOrDefault(p =>
            p.Consultas.Contains(consulta));

        if (paciente == null) return;

        await Shell.Current.GoToAsync(
            nameof(CancelarConsulta),
            new Dictionary<string, object>
            {
                { "ConsultaId", consulta.Id },
                { "Medico", consulta.Medico },
                { "Especialidade", consulta.Especialidade },
                { "Paciente", paciente.Nome },
                { "Data", consulta.DataFormatada },
                { "Horario", consulta.HorarioFormatado }
            });
    }

    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}