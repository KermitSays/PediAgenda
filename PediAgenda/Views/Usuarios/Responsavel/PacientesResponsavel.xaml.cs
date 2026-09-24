using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class PacientesResponsavel : ContentPage
{
    public ObservableCollection<Paciente> PacientesLista { get; set; }

    public PacientesResponsavel()
    {
        InitializeComponent();

        PacientesLista = new ObservableCollection<Paciente>
        {
            new Paciente
            {
                Nome = "Lucas",
                DataNascimento = new DateTime(2018, 4, 12),
                Foto = "lucas.png"
            },

            new Paciente
            {
                Nome = "Matheus",
                DataNascimento = new DateTime(2016, 8, 25),
                Foto = "matheus.png"
            },

            new Paciente
            {
                Nome = "Sofia",
                DataNascimento = new DateTime(2020, 9, 27),
                Foto = "sofia.png"
            }
        };

        BindingContext = this;
    }

    // Modelo do paciente
    public class Paciente
    {
        public string Nome { get; set; } = string.Empty;

        public DateTime DataNascimento { get; set; }

        public string Foto { get; set; } = string.Empty;

        public int Idade
        {
            get
            {
                DateTime hoje = DateTime.Today;

                int idade = hoje.Year - DataNascimento.Year;

                if (DataNascimento.Date > hoje.AddYears(-idade))
                    idade--;

                return idade;
            }
        }
    }
}