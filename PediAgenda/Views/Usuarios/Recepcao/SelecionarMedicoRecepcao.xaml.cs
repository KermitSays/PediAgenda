using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class SelecionarMedicoRecepcao : ContentPage
{
    private readonly List<MedicoSelecaoItem>
        todosMedicos;


    private TaskCompletionSource<MedicoSelecaoItem?>?
        resultadoSelecao;


    public ObservableCollection<MedicoSelecaoItem>
        MedicosFiltrados
    {
        get;
        set;
    } =
        new();


    public SelecionarMedicoRecepcao()
    {
        InitializeComponent();


        todosMedicos =
            AgendaMedicaRecepcaoDados
                .MedicosCompartilhados
                .Select(nome =>
                    new MedicoSelecaoItem
                    {
                        IdMedico =
                            AgendaMedicaRecepcaoDados
                                .ObterIdMedico(
                                    nome),

                        Nome =
                            nome,

                        // Temporário enquanto estes
                        // dados ainda não vêm da API.
                        Especialidade =
                            "Pediatria"
                    })
                .OrderBy(m =>
                    m.Nome)
                .ToList();


        MedicosCollectionView.ItemsSource =
            MedicosFiltrados;


        CarregarMedicos(
            string.Empty);
    }


    // =============================================
    // ABRIR
    // =============================================

    public async Task<MedicoSelecaoItem?>
        AbrirAsync(
            Page paginaOrigem)
    {
        resultadoSelecao =
            new TaskCompletionSource<
                MedicoSelecaoItem?>();


        await paginaOrigem.Navigation
            .PushModalAsync(
                this);


        return await resultadoSelecao.Task;
    }


    // =============================================
    // BUSCA
    // =============================================

    private void BuscaMedicoSearchBar_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        CarregarMedicos(
            e.NewTextValue
            ?? string.Empty);
    }


    private void CarregarMedicos(
        string pesquisa)
    {
        MedicosFiltrados.Clear();


        string termo =
            pesquisa.Trim();


        IEnumerable<MedicoSelecaoItem>
            medicos =
                todosMedicos;


        if (
            !string.IsNullOrWhiteSpace(
                termo))
        {
            medicos =
                medicos.Where(m =>
                    m.Nome.Contains(
                        termo,
                        StringComparison.OrdinalIgnoreCase)

                    ||

                    m.Especialidade.Contains(
                        termo,
                        StringComparison.OrdinalIgnoreCase));
        }


        foreach (
            MedicoSelecaoItem medico
            in medicos)
        {
            MedicosFiltrados.Add(
                medico);
        }
    }


    // =============================================
    // SELECIONAR
    // =============================================

    private async void MedicosCollectionView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (
            e.CurrentSelection.FirstOrDefault()
            is not MedicoSelecaoItem medico)
        {
            return;
        }


        MedicosCollectionView.SelectedItem =
            null;


        resultadoSelecao?
            .TrySetResult(
                medico);


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


    protected override bool OnBackButtonPressed()
    {
        resultadoSelecao?
            .TrySetResult(
                null);


        return base.OnBackButtonPressed();
    }


    // =============================================
    // ITEM
    // =============================================

    public class MedicoSelecaoItem
    {
        public int IdMedico
        {
            get;
            set;
        }


        public string Nome
        {
            get;
            set;
        } =
            string.Empty;


        public string Especialidade
        {
            get;
            set;
        } =
            string.Empty;
    }
}