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
                PrimeiraConsulta = false,
                UltimaConsulta = new DateTime(2026, 06, 02)
            },

            new PacienteMedico
            {
                Nome = "Maria Alice",
                Foto = "paciente2.png",
                PrimeiraConsulta = false,
                UltimaConsulta = new DateTime(2026, 05, 18)
            },

            new PacienteMedico
            {
                Nome = "Renata Oliveira",
                Foto = "paciente3.png",
                PrimeiraConsulta = true
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

    // Modelo do paciente
    public class PacienteMedico
    {
        public string Nome { get; set; } = string.Empty;

        public string Foto { get; set; } = string.Empty;

        public bool PrimeiraConsulta { get; set; }

        public DateTime? UltimaConsulta { get; set; }

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