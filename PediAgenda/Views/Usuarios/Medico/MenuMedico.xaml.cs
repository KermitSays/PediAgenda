using System.Collections.ObjectModel;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Medico;

public partial class MenuMedico : ContentPage
{
    public ObservableCollection<ConsultaHoje>
        ConsultasLista
    {
        get;
        set;
    } = new();


    public MenuMedico()
    {
        InitializeComponent();


        BindingContext =
            this;
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        AtualizarSaudacao();


        AtualizarData();


        CarregarConsultasHoje();
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

                ? "Olá, Dr(a).!"

                : $"Olá, Dr(a). {nome}!";
    }


    // =============================================
    // DATA ATUAL
    // =============================================

    private void AtualizarData()
    {
        DataAtualLabel.Text =
            $"Hoje é " +
            $"{DateTime.Today:dd 'de' MMMM 'de' yyyy}";
    }


    // =============================================
    // CONSULTAS DE HOJE
    // =============================================

    private void CarregarConsultasHoje()
    {
        ConsultasLista.Clear();


        List<ConsultaMedico>
            consultasHoje =
                ConsultasMedicoDados
                    .ObterConsultasAtivasPorData(
                        DateTime.Today);


        foreach (
            ConsultaMedico consulta
            in consultasHoje)
        {
            ConsultasLista.Add(
                new ConsultaHoje
                {
                    IdConsulta =
                        consulta.Id,

                    Horario =
                        consulta.HorarioTexto,

                    Paciente =
                        consulta.Paciente,

                    Status =
                        consulta.Status
                });
        }


        bool temConsultas =
            ConsultasLista.Count > 0;


        ConsultasCollectionView.IsVisible =
            temConsultas;


        SemConsultasContainer.IsVisible =
            !temConsultas;
    }

 
    // =============================================
    // PACIENTES
    // =============================================

    private async void MedicoPacientesButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(PacientesMedico));
    }


    // =============================================
    // CONSULTAS
    // =============================================

    private async void MedicoConsultasButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(ConsultasMedico));
    }


    // =============================================
    // RELATÓRIOS
    // =============================================

    private async void MedicoRelatoriosButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(RelatoriosMedico));
    }
}


// =============================================
// MODELO VISUAL
// =============================================

public class ConsultaHoje
{
    public Guid IdConsulta
    {
        get;
        set;
    }


    public string Horario
    {
        get;
        set;
    } = string.Empty;


    public string Paciente
    {
        get;
        set;
    } = string.Empty;


    public string Status
    {
        get;
        set;
    } = string.Empty;
}