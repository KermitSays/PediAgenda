using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
public partial class AgendarMedico : ContentPage
{
    private int pacienteId;

    private string? paciente;

    private string? especialidade;

    private Medico? medicoSelecionado;


    public int PacienteId
    {
        get => pacienteId;

        set => pacienteId = value;
    }


    public string Paciente
    {
        get => paciente ?? string.Empty;

        set => paciente = value;
    }


    public string Especialidade
    {
        get => especialidade ?? string.Empty;

        set
        {
            especialidade = value;


            if (EspecialidadeLabel != null)
            {
                EspecialidadeLabel.Text =
                    $"Especialidade: {value}";
            }


            CarregarMedicos(value);
        }
    }


    public AgendarMedico()
    {
        InitializeComponent();
    }


    // CARREGA OS MÉDICOS

    private void CarregarMedicos(
        string especialidade)
    {
        List<Medico> medicos =
            especialidade switch
            {
                "Pediatria" =>
                    new List<Medico>
                    {
                        new Medico
                        {
                            IdMedico = 1,
                            Nome = "Dra. Ana Oliveira",
                            Rqe = "RQE 12345",
                            Foto = "user.png"
                        },

                        new Medico
                        {
                            IdMedico = 2,
                            Nome = "Dr. Carlos Mendes",
                            Rqe = "RQE 23456",
                            Foto = "user.png"
                        },

                        new Medico
                        {
                            IdMedico = 3,
                            Nome = "Dra. Juliana Santos",
                            Rqe = "RQE 34567",
                            Foto = "user.png"
                        }
                    },


                "Neuropediatria" =>
                    new List<Medico>
                    {
                        new Medico
                        {
                            IdMedico = 4,
                            Nome = "Dra. Mariana Costa",
                            Rqe = "RQE 45678",
                            Foto = "user.png"
                        },

                        new Medico
                        {
                            IdMedico = 5,
                            Nome = "Dr. Rafael Almeida",
                            Rqe = "RQE 56789",
                            Foto = "user.png"
                        }
                    },


                "Cardiologia" =>
                    new List<Medico>
                    {
                        new Medico
                        {
                            IdMedico = 6,
                            Nome = "Dr. Felipe Martins",
                            Rqe = "RQE 67890",
                            Foto = "user.png"
                        },

                        new Medico
                        {
                            IdMedico = 7,
                            Nome = "Dra. Camila Rodrigues",
                            Rqe = "RQE 78901",
                            Foto = "user.png"
                        }
                    },


                "Endocrinologia" =>
                    new List<Medico>
                    {
                        new Medico
                        {
                            IdMedico = 8,
                            Nome = "Dra. Beatriz Lima",
                            Rqe = "RQE 89012",
                            Foto = "user.png"
                        },

                        new Medico
                        {
                            IdMedico = 9,
                            Nome = "Dr. Lucas Ferreira",
                            Rqe = "RQE 90123",
                            Foto = "user.png"
                        }
                    },


                _ =>
                    new List<Medico>()
            };


        MedicosCollectionView.ItemsSource =
            medicos;
    }


    // SELECIONA O MÉDICO

    private void MedicoCard_Tapped(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not Border border)
            return;


        if (border.BindingContext
            is not Medico medico)
        {
            return;
        }


        if (MedicosCollectionView.ItemsSource
            is not List<Medico> medicos)
        {
            return;
        }


        foreach (Medico item in medicos)
        {
            item.Selecionado =
                item == medico;
        }


        medicoSelecionado =
            medico;


        ErroLabel.IsVisible =
            false;
    }


    // CONTINUAR

    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (medicoSelecionado == null)
        {
            ErroLabel.Text =
                "Selecione um médico para continuar.";

            ErroLabel.IsVisible =
                true;

            return;
        }


        await Shell.Current.GoToAsync(
            nameof(AgendarDataHorario),
            new Dictionary<string, object>
            {
                {
                    "PacienteId",
                    PacienteId
                },

                {
                    "Paciente",
                    Paciente
                },

                {
                    "MedicoId",
                    medicoSelecionado.IdMedico
                },

                {
                    "Medico",
                    medicoSelecionado.Nome
                },

                {
                    "FotoMedico",
                    medicoSelecionado.Foto
                },

                {
                    "Especialidade",
                    Especialidade
                }
            });
    }


    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}


// MODELO DO MÉDICO

public class Medico : INotifyPropertyChanged
{
    private bool selecionado;


    public int IdMedico { get; set; }


    public string Nome { get; set; } =
        string.Empty;


    public string Rqe { get; set; } =
        string.Empty;


    public string Foto { get; set; } =
        string.Empty;


    public bool Selecionado
    {
        get => selecionado;

        set
        {
            if (selecionado == value)
                return;


            selecionado =
                value;


            OnPropertyChanged();

            OnPropertyChanged(
                nameof(CorFundo));

            OnPropertyChanged(
                nameof(CorBorda));

            OnPropertyChanged(
                nameof(EspessuraBorda));
        }
    }


    public Color CorFundo =>
        Selecionado
            ? Color.FromArgb("#EAF5FA")
            : Color.FromArgb("#FFFFFF");


    public Color CorBorda =>
        Selecionado
            ? Color.FromArgb("#105A7E")
            : Color.FromArgb("#ABCCDC");


    public double EspessuraBorda =>
        Selecionado
            ? 2.5
            : 1;


    public event PropertyChangedEventHandler?
        PropertyChanged;


    protected void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}