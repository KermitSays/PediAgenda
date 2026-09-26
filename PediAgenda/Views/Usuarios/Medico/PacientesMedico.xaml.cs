using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class PacientesMedico : ContentPage
{
    public ObservableCollection<PacienteMedico> Pacientes { get; set; }

    public ObservableCollection<PacienteMedico> PacientesFiltrados { get; set; }

    public PacientesMedico()
    {
        InitializeComponent();

        // Pacientes mockados
        Pacientes = new ObservableCollection<PacienteMedico>
    {
        new PacienteMedico
        {
            Nome = "João da Silva",
            Foto = "paciente1.png",
            DataNascimento = new DateTime(2018, 5, 12),
            Responsavel = "Carlos da Silva",
            PrimeiraConsulta = false,
            UltimaConsulta = new DateTime(2026, 6, 2),

            Historico = new ObservableCollection<ProntuarioPaciente.ConsultaHistorico>
            {
                new ProntuarioPaciente.ConsultaHistorico
                {
                    TipoConsulta = "Consulta pediátrica",
                    Data = new DateTime(2026, 6, 2),
                    Observacoes =
                        "Paciente compareceu acompanhado do responsável. " +
                        "Relatado quadro de febre e coriza nos últimos dias. " +
                        "Realizada avaliação clínica durante a consulta. " +
                        "Orientações registradas conforme avaliação médica."
                },

                new ProntuarioPaciente.ConsultaHistorico
                {
                    TipoConsulta = "Consulta pediátrica",
                    Data = new DateTime(2026, 4, 5),
                    Observacoes =
                        "Consulta de acompanhamento. Responsável relata " +
                        "melhora dos sintomas apresentados anteriormente. " +
                        "Paciente avaliado durante a consulta e acompanhamento mantido."
                }
            }
        },

        new PacienteMedico
        {
            Nome = "Maria Alice",
            Foto = "paciente2.png",
            DataNascimento = new DateTime(2020, 8, 25),
            Responsavel = "Fernanda Alice",
            PrimeiraConsulta = false,
            UltimaConsulta = new DateTime(2026, 5, 18),

            Historico = new ObservableCollection<ProntuarioPaciente.ConsultaHistorico>
            {
                new ProntuarioPaciente.ConsultaHistorico
                {
                    TipoConsulta = "Consulta pediátrica",
                    Data = new DateTime(2026, 5, 18),
                    Observacoes =
                        "Paciente compareceu para avaliação de rotina " +
                        "acompanhada do responsável. Realizada avaliação clínica " +
                        "e registradas as orientações referentes à consulta."
                }
            }
        },

        new PacienteMedico
        {
            Nome = "Renata Oliveira",
            Foto = "paciente3.png",
            DataNascimento = new DateTime(2017, 11, 3),
            Responsavel = "Marcos Oliveira",
            PrimeiraConsulta = true,

            Historico = new ObservableCollection<ProntuarioPaciente.ConsultaHistorico>()
        }
    };

        // Inicialmente mostra todos
        PacientesFiltrados =
            new ObservableCollection<PacienteMedico>(Pacientes);

        BindingContext = this;
    }

    // Pesquisa pacientes pelo nome
    private void BuscarPacienteEntry_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        string textoBusca = e.NewTextValue?.Trim() ?? string.Empty;

        PacientesFiltrados.Clear();

        foreach (PacienteMedico paciente in Pacientes)
        {
            if (string.IsNullOrWhiteSpace(textoBusca) ||
                paciente.Nome.Contains(
                    textoBusca,
                    StringComparison.OrdinalIgnoreCase))
            {
                PacientesFiltrados.Add(paciente);
            }
        }
    }

    // Abre o prontuário do paciente
    private async void PacientesMedico_Clicked(
        object sender,
        TappedEventArgs e)
    {
        if (sender is Border border &&
            border.BindingContext is PacienteMedico paciente)
        {
            await Shell.Current.GoToAsync(
                nameof(ProntuarioPaciente),
                new Dictionary<string, object>
                {
                {
                    "NomePaciente",
                    paciente.Nome
                }
                });
        }
    }

    // Modelo do paciente
    public class PacienteMedico
    {
        public string Nome { get; set; } = string.Empty;

        public string Foto { get; set; } = string.Empty;

        public bool PrimeiraConsulta { get; set; }

        public DateTime? UltimaConsulta { get; set; }

        public DateTime DataNascimento { get; set; }

        public string Responsavel { get; set; } = string.Empty;

        public ObservableCollection<ProntuarioPaciente.ConsultaHistorico> Historico
        { get; set; } = new();

        public string UltimaConsultaTexto
        {
            get
            {
                if (PrimeiraConsulta || !UltimaConsulta.HasValue)
                {
                    return "Primeira consulta";
                }

                return $"Última consulta: {UltimaConsulta.Value:dd/MM/yyyy}";
            }
        }
    }
}