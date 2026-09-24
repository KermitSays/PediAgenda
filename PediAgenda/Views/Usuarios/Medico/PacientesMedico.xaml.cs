using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class PacientesMedico : ContentPage
{
    public ObservableCollection<PacienteMedico> PacientesLista { get; set; }

    public PacientesMedico()
    {
        InitializeComponent();

        // Pacientes mockados
        PacientesLista = new ObservableCollection<PacienteMedico>
        {
            new PacienteMedico
            {
                Nome = "João Silva",
                DataNascimento = new DateTime(2018, 5, 12),
                Foto = "paciente1.png"
            },

            new PacienteMedico
            {
                Nome = "Maria Alice",
                DataNascimento = new DateTime(2020, 8, 25),
                Foto = "paciente2.png"
            },

            new PacienteMedico
            {
                Nome = "Renata Oliveira",
                DataNascimento = new DateTime(2017, 11, 3),
                Foto = "paciente3.png"
            }
        };

        BindingContext = this;
    }

    // Modelo do paciente
    public class PacienteMedico
    {
        public string Nome { get; set; } = string.Empty;

        public DateTime DataNascimento { get; set; }

        public string Foto { get; set; } = string.Empty;

        // Calcula a idade automaticamente
        public int Idade
        {
            get
            {
                DateTime hoje = DateTime.Today;

                int idade = hoje.Year - DataNascimento.Year;

                if (DataNascimento.Date > hoje.AddYears(-idade))
                {
                    idade--;
                }

                return idade;
            }
        }

        // Texto exibido para a idade
        public string IdadeTexto =>
            $"{Idade} anos";

        // Texto exibido para a data de nascimento
        public string DataNascimentoTexto =>
            $"Nascimento: {DataNascimento:dd/MM/yyyy}";
    }
}