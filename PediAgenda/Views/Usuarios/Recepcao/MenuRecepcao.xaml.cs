using System.Collections.ObjectModel;
using PediAgenda.Dados;
using PediAgenda.Views.Usuarios.Medico;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class MenuRecepcao : ContentPage
{
    public ObservableCollection<ConsultaRecepcaoResumo>
        ConsultasHoje
    {
        get;
        set;
    }


    public MenuRecepcao()
    {
        InitializeComponent();


        ConsultasHoje =
            new ObservableCollection<ConsultaRecepcaoResumo>
            {
                new ConsultaRecepcaoResumo
                {
                    Horario =
                        "08:00",

                    Paciente =
                        "Maria Alice",

                    Medico =
                        "Dr. Carlos Mendes",

                    Status =
                        "Confirmado"
                },


                new ConsultaRecepcaoResumo
                {
                    Horario =
                        "09:00",

                    Paciente =
                        "João Pedro",

                    Medico =
                        "Dra. Fernanda Lima",

                    Status =
                        "Por Confirmar"
                },


                new ConsultaRecepcaoResumo
                {
                    Horario =
                        "10:00",

                    Paciente =
                        "Bianca",

                    Medico =
                        "Dr. Carlos Mendes",

                    Status =
                        "Confirmado"
                },


                new ConsultaRecepcaoResumo
                {
                    Horario =
                        "11:00",

                    Paciente =
                        "Isaac",

                    Medico =
                        "Dra. Fernanda Lima",

                    Status =
                        "Cancelado"
                }
            };


        BindingContext =
            this;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        AtualizarSaudacao();


        AtualizarData();


        AtualizarQuantidadeConsultas();
    }


    // =============================================
    // SAUDAÇÃO
    // =============================================

    private void AtualizarSaudacao()
    {
        string nome =
            SessaoUsuario.PrimeiroNome;


        SaudacaoLabel.Text =
            string.IsNullOrWhiteSpace(nome)

                ? "Olá!"

                : $"Olá, {nome}!";
    }


    // =============================================
    // DATA
    // =============================================

    private void AtualizarData()
    {
        DataAtualLabel.Text =
            $"Hoje é " +
            $"{DateTime.Today:dd 'de' MMMM 'de' yyyy}";
    }


    // =============================================
    // QUANTIDADE DE CONSULTAS
    // =============================================

    private void AtualizarQuantidadeConsultas()
    {
        QuantidadeConsultasLabel.Text =
            $"{ConsultasHoje.Count} consulta(s)";
    }


    // =============================================
    // PACIENTES
    // =============================================

    private async void PacientesButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(PacientesRecepcao));
    }


    // =============================================
    // MÉDICOS
    // =============================================

    private async void MedicosButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MedicosRecepcao));
    }


    // =============================================
    // BOTÃO ATUAL DA TERCEIRA OPÇÃO
    // =============================================

    private async void AdicionarPacienteButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await DisplayAlertAsync(
            "Adicionar Paciente",
            "A funcionalidade de adicionar paciente será implementada no futuro.",
            "OK");
    }
}