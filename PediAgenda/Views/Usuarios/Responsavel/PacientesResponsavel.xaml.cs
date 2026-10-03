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


    // ABRE UMA CONSULTA DO HISTÓRICO

    private async void HistoricoConsultaCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not BindableObject elemento ||
            elemento.BindingContext
                is not ConsultaResponsavelItem consulta)
        {
            return;
        }


        PacienteResponsavelItem? paciente =
            PacientesLista.FirstOrDefault(
                p =>
                    p.Consultas.Contains(
                        consulta));


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


    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}