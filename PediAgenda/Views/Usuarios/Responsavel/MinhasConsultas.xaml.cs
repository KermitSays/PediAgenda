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

        // Força a atualização da lista quando volta
        // de cancelamento ou reagendamento.
        PacientesCollectionView.ItemsSource = null;

        PacientesCollectionView.ItemsSource =
            pacientes;
    }


    // TOQUE NO CARD DO PACIENTE
    private void PacienteCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is BindableObject elemento &&
            elemento.BindingContext is PacienteResponsavelItem paciente)
        {
            paciente.IsExpanded =
                !paciente.IsExpanded;
        }
    }


    // TOQUE NO CARD DA CONSULTA
    private async void ConsultaCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is BindableObject elemento &&
            elemento.BindingContext is ConsultaResponsavelItem consulta)
        {
            PacienteResponsavelItem? paciente =
                pacientes.FirstOrDefault(
                    p => p.Consultas.Contains(consulta));


            if (paciente == null)
                return;


            await Shell.Current.GoToAsync(
                nameof(MinhasConsultasDetalhes),
                new Dictionary<string, object>
                {
                    {
                        "ConsultaId",
                        consulta.Id
                    },

                    {
                        "Medico",
                        consulta.Medico
                    },

                    {
                        "Especialidade",
                        consulta.Especialidade
                    },

                    {
                        "Paciente",
                        paciente.Nome
                    },

                    {
                        "Data",
                        consulta.DataFormatada
                    },

                    {
                        "Horario",
                        consulta.HorarioFormatado
                    },

                    {
                        "Modalidade",
                        consulta.Modalidade
                    },

                    {
                        "Valor",
                        consulta.Valor
                    },

                    {
                        "Status",
                        consulta.Status
                    }
                });
        }
    }


    // VOLTAR
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}