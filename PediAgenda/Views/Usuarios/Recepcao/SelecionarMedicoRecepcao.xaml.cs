
using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class SelecionarMedicoRecepcao : ContentPage
{
    private readonly List<MedicoSelecaoItem> todosMedicos = new();

    private TaskCompletionSource<MedicoSelecaoItem?>?
        resultadoSelecao;

    public ObservableCollection<MedicoSelecaoItem>
        MedicosFiltrados
    { get; } = new();

    public SelecionarMedicoRecepcao()
    {
        InitializeComponent();

        MedicosCollectionView.ItemsSource = MedicosFiltrados;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var medicosApi =
                await AgendaRecepcaoApi.ListarMedicosAsync();

            todosMedicos.Clear();

            foreach (var medico in medicosApi)
            {
                todosMedicos.Add(new MedicoSelecaoItem
                {
                    IdMedico = medico.IdMedico,
                    Nome = medico.Nome,
                    Especialidade = medico.Especialidade
                });
            }

            CarregarMedicos(
                BuscaMedicoSearchBar.Text ?? "");
        }
        catch (Exception ex)
        {
            todosMedicos.Clear();
            CarregarMedicos("");

            await DisplayAlertAsync(
                "Erro ao carregar médicos",
                $"Não foi possível consultar os médicos: {ex.Message}",
                "OK");
        }
    }

    public async Task<MedicoSelecaoItem?> AbrirAsync(
        Page paginaOrigem)
    {
        resultadoSelecao =
            new TaskCompletionSource<MedicoSelecaoItem?>();

        await paginaOrigem.Navigation.PushModalAsync(this);

        return await resultadoSelecao.Task;
    }

    private void BuscaMedicoSearchBar_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        CarregarMedicos(e.NewTextValue ?? "");
    }

    private void CarregarMedicos(string pesquisa)
    {
        MedicosFiltrados.Clear();

        string termo = pesquisa.Trim();

        var medicos = todosMedicos
            .Where(m =>
                string.IsNullOrWhiteSpace(termo) ||
                m.Nome.Contains(
                    termo,
                    StringComparison.OrdinalIgnoreCase) ||
                m.Especialidade.Contains(
                    termo,
                    StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.Nome);

        foreach (var medico in medicos)
        {
            MedicosFiltrados.Add(medico);
        }
    }

    private async void MedicosCollectionView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault()
            is not MedicoSelecaoItem medico)
        {
            return;
        }

        MedicosCollectionView.SelectedItem = null;

        resultadoSelecao?.TrySetResult(medico);

        await Navigation.PopModalAsync();
    }

    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        resultadoSelecao?.TrySetResult(null);

        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        resultadoSelecao?.TrySetResult(null);

        return base.OnBackButtonPressed();
    }

    public class MedicoSelecaoItem
    {
        public int IdMedico { get; set; }

        public string Nome { get; set; } = "";

        public string Especialidade { get; set; } = "";
    }
}
