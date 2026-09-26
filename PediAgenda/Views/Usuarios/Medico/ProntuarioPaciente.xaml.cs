using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(NomePaciente), "NomePaciente")]
public partial class ProntuarioPaciente : ContentPage
{
    private string nomePaciente = string.Empty;

    public string NomePaciente
    {
        get => nomePaciente;

        set
        {
            nomePaciente = value;

            if (NomePacienteLabel != null)
            {
                CarregarPaciente();
            }
        }
    }

    public ProntuarioPaciente()
    {
        InitializeComponent();
    }

    // Carrega os dados do paciente
    private void CarregarPaciente()
    {
        NomePacienteLabel.Text = NomePaciente;

        // Dados mockados
        if (NomePaciente == "João da Silva")
        {
            DataNascimentoLabel.Text = "12/05/2018";
            ResponsavelLabel.Text = "Carlos da Silva";

            HistoricoCollectionView.ItemsSource =
                new ObservableCollection<ConsultaHistorico>
                {
                    new ConsultaHistorico
                    {
                        TipoConsulta = "Consulta pediátrica",
                        Data = new DateTime(2026, 6, 2),
                        Observacoes =
                            "Paciente compareceu acompanhado do responsável. " +
                            "Relatado quadro de febre e coriza nos últimos dias. " +
                            "Realizada avaliação clínica durante a consulta. " +
                            "Orientações registradas conforme avaliação médica."
                    },
                    new ConsultaHistorico
                    {
                        TipoConsulta = "Consulta pediátrica",
                        Data = new DateTime(2026, 4, 5),
                        Observacoes =
                            "Consulta de acompanhamento. Responsável relata " +
                            "melhora dos sintomas apresentados anteriormente. " +
                            "Paciente avaliado durante a consulta e acompanhamento mantido."
                    }
                };
        }
        else if (NomePaciente == "Maria Alice")
        {
            DataNascimentoLabel.Text = "25/08/2020";
            ResponsavelLabel.Text = "Fernanda Alice";

            HistoricoCollectionView.ItemsSource =
                new ObservableCollection<ConsultaHistorico>
                {
                    new ConsultaHistorico
                    {
                        TipoConsulta = "Consulta pediátrica",
                        Data = new DateTime(2026, 5, 18),
                        Observacoes =
                            "Paciente compareceu para avaliação de rotina " +
                            "acompanhada do responsável. Realizada avaliação clínica " +
                            "e registradas as orientações referentes à consulta."
                    }
                };
        }
        else if (NomePaciente == "Renata Oliveira")
        {
            DataNascimentoLabel.Text = "03/11/2017";
            ResponsavelLabel.Text = "Marcos Oliveira";

            HistoricoCollectionView.ItemsSource =
                new ObservableCollection<ConsultaHistorico>();
        }
    }

    // Volta para a lista de pacientes
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    // Modelo do histórico
    public class ConsultaHistorico
    {
        public string TipoConsulta { get; set; } = string.Empty;
        public DateTime Data { get; set; }
        public string Observacoes { get; set; } = string.Empty;

        public string DataTexto => Data.ToString("dd/MM/yyyy");
    }

    // Abre as observações da consulta
    private async void ConsultaHistorico_Clicked(
        object sender,
        TappedEventArgs e)
    {
        if (sender is Border border &&
            border.BindingContext is ConsultaHistorico consulta)
        {
            await Shell.Current.GoToAsync(
                nameof(DetalhesProntuario),
                new Dictionary<string, object>
                {
                {
                    "TipoConsulta",
                    consulta.TipoConsulta
                },
                {
                    "DataConsulta",
                    consulta.Data.ToString("dd/MM/yyyy")
                },
                {
                    "Observacoes",
                    consulta.Observacoes
                }
                });
        }
    }
}