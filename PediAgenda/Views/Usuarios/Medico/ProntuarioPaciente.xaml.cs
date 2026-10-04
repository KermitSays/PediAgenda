namespace PediAgenda.Views.Usuarios.Medico;

[QueryProperty(nameof(IdPaciente), "IdPaciente")]
public partial class ProntuarioPaciente : ContentPage
{
    private int idPaciente;

    private PacienteMedicoItem?
        pacienteAtual;


    public int IdPaciente
    {
        get => idPaciente;

        set
        {
            idPaciente =
                value;


            CarregarPaciente();
        }
    }


    public ProntuarioPaciente()
    {
        InitializeComponent();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();


        CarregarPaciente();
    }


    // CARREGA OS DADOS DO PACIENTE

    private void CarregarPaciente()
    {
        if (IdPaciente <= 0)
            return;


        pacienteAtual =
            MedicoDados
                .ObterPacientePorId(
                    IdPaciente);


        if (pacienteAtual == null)
        {
            NomePacienteLabel.Text =
                "Paciente não encontrado";


            DataNascimentoLabel.Text =
                "-";


            ResponsavelLabel.Text =
                "-";


            HistoricoCollectionView.ItemsSource =
                null;


            HistoricoCollectionView.IsVisible =
                false;


            SemHistoricoLabel.IsVisible =
                true;


            return;
        }


        NomePacienteLabel.Text =
            pacienteAtual.Nome;


        DataNascimentoLabel.Text =
            pacienteAtual
                .DataNascimento
                .ToString(
                    "dd/MM/yyyy");


        ResponsavelLabel.Text =
            pacienteAtual.Responsavel;


        List<ConsultaHistoricoMedicoItem>
            historicoOrdenado =
                pacienteAtual
                    .Historico
                    .OrderByDescending(h =>
                        h.Data)
                    .ToList();


        HistoricoCollectionView.ItemsSource =
            historicoOrdenado;


        SemHistoricoLabel.IsVisible =
            historicoOrdenado.Count == 0;


        HistoricoCollectionView.IsVisible =
            historicoOrdenado.Count > 0;
    }


    // ABRE OS DETALHES DO REGISTRO

    private async void ConsultaHistorico_Clicked(
        object sender,
        TappedEventArgs e)
    {
        if (sender is not Border border)
            return;


        if (border.BindingContext
            is not ConsultaHistoricoMedicoItem consulta)
        {
            return;
        }


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
                    consulta.Data.ToString(
                        "dd/MM/yyyy")
                },

                {
                    "Observacoes",
                    consulta.Observacoes
                }
            });
    }


    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            "..");
    }
}