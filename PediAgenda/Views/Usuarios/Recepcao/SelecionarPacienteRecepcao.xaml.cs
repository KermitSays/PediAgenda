using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class SelecionarPacienteRecepcao : ContentPage
{
    private readonly List<PacienteSelecaoItem>
        todosPacientes;


    private TaskCompletionSource<PacienteRecepcaoItem?>?
        resultadoSelecao;


    public ObservableCollection<PacienteSelecaoItem>
        PacientesFiltrados
    {
        get;
        set;
    } =
        new();


    public SelecionarPacienteRecepcao()
    {
        InitializeComponent();


        todosPacientes =
            PacientesRecepcaoDados.Pacientes
                .Where(p =>
                    p.Status.Equals(
                        "Ativo",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(p =>
                    p.Nome)
                .Select(p =>
                    new PacienteSelecaoItem(
                        p))
                .ToList();


        PacientesCollectionView.ItemsSource =
            PacientesFiltrados;


        CarregarPacientes(
            string.Empty);
    }


    // =============================================
    // ABRIR SELETOR
    // =============================================

    public async Task<PacienteRecepcaoItem?>
        AbrirAsync(
            Page paginaOrigem)
    {
        resultadoSelecao =
            new TaskCompletionSource<
                PacienteRecepcaoItem?>();


        await paginaOrigem.Navigation
            .PushModalAsync(
                this);


        return await resultadoSelecao.Task;
    }


    // =============================================
    // PESQUISA
    // =============================================

    private void BuscaPacienteSearchBar_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        CarregarPacientes(
            e.NewTextValue
            ?? string.Empty);
    }


    private void CarregarPacientes(
        string pesquisa)
    {
        PacientesFiltrados.Clear();


        string termo =
            pesquisa
                .Trim();


        IEnumerable<PacienteSelecaoItem>
            pacientes =
                todosPacientes;


        if (
            !string.IsNullOrWhiteSpace(
                termo))
        {
            pacientes =
                pacientes.Where(item =>
                    item.Paciente.Nome.Contains(
                        termo,
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    item.Paciente.Responsavel.Contains(
                        termo,
                        StringComparison.OrdinalIgnoreCase));
        }


        foreach (
            PacienteSelecaoItem paciente
            in pacientes)
        {
            PacientesFiltrados.Add(
                paciente);
        }
    }


    // =============================================
    // PACIENTE SELECIONADO
    // =============================================

    private async void PacientesCollectionView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (
            e.CurrentSelection.FirstOrDefault()
            is not PacienteSelecaoItem item)
        {
            return;
        }


        PacientesCollectionView.SelectedItem =
            null;


        resultadoSelecao?
            .TrySetResult(
                item.Paciente);


        await Navigation
            .PopModalAsync();
    }


    // =============================================
    // CANCELAR
    // =============================================

    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        resultadoSelecao?
            .TrySetResult(
                null);


        await Navigation
            .PopModalAsync();
    }


    // =============================================
    // VOLTAR DO ANDROID
    // =============================================

    protected override bool OnBackButtonPressed()
    {
        resultadoSelecao?
            .TrySetResult(
                null);


        return base.OnBackButtonPressed();
    }


    // =============================================
    // ITEM VISUAL
    // =============================================

    public class PacienteSelecaoItem
    {
        public PacienteRecepcaoItem Paciente
        {
            get;
        }


        public PacienteSelecaoItem(
            PacienteRecepcaoItem paciente)
        {
            Paciente =
                paciente;
        }


        public string NomeIdade
        {
            get
            {
                int idade =
                    CalcularIdade(
                        Paciente.DataNascimento);


                string textoIdade =
                    idade == 1
                        ? "1 ano"
                        : $"{idade} anos";


                return
                    $"{Paciente.Nome} — {textoIdade}";
            }
        }


        public string ResponsavelTexto =>
            $"Responsável: {Paciente.Responsavel}";


        public string NascimentoTexto =>
            $"Nascimento: {Paciente.DataNascimento:dd/MM/yyyy}";


        private static int CalcularIdade(
            DateTime nascimento)
        {
            DateTime hoje =
                DateTime.Today;


            int idade =
                hoje.Year -
                nascimento.Year;


            if (
                nascimento.Date >
                hoje.AddYears(
                    -idade))
            {
                idade--;
            }


            return Math.Max(
                idade,
                0);
        }
    }
}