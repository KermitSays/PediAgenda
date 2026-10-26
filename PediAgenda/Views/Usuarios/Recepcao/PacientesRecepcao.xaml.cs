using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class PacientesRecepcao : ContentPage
{
    private List<PacienteRecepcaoItem> _todosPacientes;

    private bool _exibindoPacientesReais = false;

    public ObservableCollection<PacienteRecepcaoItem> PacientesFiltrados { get; set; }

    public PacientesRecepcao()
    {
        InitializeComponent();

        _todosPacientes = PacientesRecepcaoDados.Pacientes.ToList();

        PacientesFiltrados =
            new ObservableCollection<PacienteRecepcaoItem>(_todosPacientes);

        BindingContext = this;
    }



    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            _todosPacientes.Clear();
            PacientesFiltrados.Clear();

            await CarregarPacientesDaApiAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "Erro ao carregar pacientes",
                "Não foi possível consultar os pacientes pela API. Verifique a conexão e o login da Recepção.",
                "OK");
        }
    }



    private static PacienteRecepcaoItem ConverterPacienteDaApi(
        PacienteRecepcaoRespostaApi origem)
    {
        return new PacienteRecepcaoItem
        {
            Id = origem.Id,
            Nome = origem.Nome,
            DataNascimento = DateTime.ParseExact(
                origem.DataNascimento,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture),
            Responsavel = origem.Responsavel,
            TelefoneResponsavel = origem.TelefoneResponsavel,
            EmailResponsavel = origem.EmailResponsavel ?? string.Empty,
            Status = "Cadastrado"
        };
    }


    private async Task CarregarPacientesDaApiAsync()
    {
        var pacientesApi =
            await PacientesRecepcaoApi.ListarAsync();

        var pacientes =
            pacientesApi
                .Select(ConverterPacienteDaApi)
                .ToList();

        _todosPacientes = pacientes;
        _exibindoPacientesReais = true;

        PacientesFiltrados.Clear();

        foreach (var paciente in pacientes)
        {
            PacientesFiltrados.Add(paciente);
        }
    }


    // FILTRO DE BUSCA
    private void PacienteSearchBar_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        string texto =
            e.NewTextValue?.Trim().ToLower() ?? string.Empty;

        var filtrados =
            string.IsNullOrWhiteSpace(texto)
            ? _todosPacientes
            : _todosPacientes
                .Where(p =>
                    p.Nome.ToLower().Contains(texto) ||
                    p.Responsavel.ToLower().Contains(texto))
                .ToList();

        PacientesFiltrados.Clear();

        foreach (var paciente in filtrados)
        {
            PacientesFiltrados.Add(paciente);
        }
    }


    // ACESSAR PACIENTE

    private async void DetalhesButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (sender is Button botao &&
            botao.CommandParameter is PacienteRecepcaoItem paciente)
        {
            if (_exibindoPacientesReais)
            {
                await Shell.Current.GoToAsync(
                    nameof(DetalhesPacienteRecepcao),
                    new Dictionary<string, object>
                    {
                    { "PacienteDaApi", paciente }
                    });
            }
            else
            {
                await Shell.Current.GoToAsync(
                    $"{nameof(DetalhesPacienteRecepcao)}?PacienteId={paciente.Id}");
            }
        }
    }

}