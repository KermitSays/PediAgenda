using System.Collections.ObjectModel;
using System.Globalization;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class SelecionarPacienteRecepcao : ContentPage
{
    private readonly List<PacienteSelecaoItem> todosPacientes = new();
    private TaskCompletionSource<PacienteRecepcaoItem?>? resultadoSelecao;
    public ObservableCollection<PacienteSelecaoItem> PacientesFiltrados { get; } = new();

    public SelecionarPacienteRecepcao()
    {
        InitializeComponent();
        PacientesCollectionView.ItemsSource = PacientesFiltrados;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var lista = await PacientesRecepcaoApi.ListarAsync();
            todosPacientes.Clear();
            foreach (var item in lista.OrderBy(p => p.Nome))
            {
                var paciente = new PacienteRecepcaoItem
                {
                    Id = item.Id,
                    Nome = item.Nome,
                    Responsavel = item.Responsavel,
                    TelefoneResponsavel = item.TelefoneResponsavel,
                    EmailResponsavel = item.EmailResponsavel ?? string.Empty,
                    DataNascimento = DateTime.ParseExact(item.DataNascimento,
                        "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Status = "Cadastrado"
                };
                todosPacientes.Add(new PacienteSelecaoItem(paciente));
            }
            CarregarPacientes(BuscaPacienteSearchBar.Text ?? "");
        }
        catch (Exception ex)
        {
            todosPacientes.Clear();
            CarregarPacientes("");
            await DisplayAlertAsync("Erro ao carregar pacientes", ex.Message, "OK");
        }
    }

    public async Task<PacienteRecepcaoItem?> AbrirAsync(Page paginaOrigem)
    {
        resultadoSelecao = new TaskCompletionSource<PacienteRecepcaoItem?>();
        await paginaOrigem.Navigation.PushModalAsync(this);
        return await resultadoSelecao.Task;
    }

    private void BuscaPacienteSearchBar_TextChanged(object sender, TextChangedEventArgs e) =>
        CarregarPacientes(e.NewTextValue ?? "");

    private void CarregarPacientes(string pesquisa)
    {
        PacientesFiltrados.Clear();
        string termo = pesquisa.Trim();
        foreach (var item in todosPacientes.Where(i =>
            string.IsNullOrWhiteSpace(termo) ||
            i.Paciente.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
            i.Paciente.Responsavel.Contains(termo, StringComparison.OrdinalIgnoreCase)))
            PacientesFiltrados.Add(item);
    }

    private async void PacientesCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not PacienteSelecaoItem item) return;
        PacientesCollectionView.SelectedItem = null;
        resultadoSelecao?.TrySetResult(item.Paciente);
        await Navigation.PopModalAsync();
    }

    private async void CancelarButton_Clicked(object sender, EventArgs e)
    {
        resultadoSelecao?.TrySetResult(null);
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        resultadoSelecao?.TrySetResult(null);
        return base.OnBackButtonPressed();
    }

    public class PacienteSelecaoItem(PacienteRecepcaoItem paciente)
    {
        public PacienteRecepcaoItem Paciente { get; } = paciente;
        public string NomeIdade
        {
            get
            {
                int idade = DateTime.Today.Year - Paciente.DataNascimento.Year;
                if (Paciente.DataNascimento.Date > DateTime.Today.AddYears(-idade)) idade--;
                return $"{Paciente.Nome} — {Math.Max(idade, 0)} " +
                    (idade == 1 ? "ano" : "anos");
            }
        }
        public string ResponsavelTexto => $"Responsável: {Paciente.Responsavel}";
        public string NascimentoTexto => $"Nascimento: {Paciente.DataNascimento:dd/MM/yyyy}";
    }
}
