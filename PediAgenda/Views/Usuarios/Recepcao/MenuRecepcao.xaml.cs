namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class MenuRecepcao : ContentPage
{
    public MenuRecepcao()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        DataAtualLabel.Text =
            $"Hoje é {DateTime.Today:dd 'de' MMMM 'de' yyyy}";
    }

    private async void PacientesButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PacientesRecepcao));
    }

    private async void MedicosButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MedicosRecepcao));
    }

    private async void CadastrarResponsavelButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(PediAgenda.Views.Cadastro.CadastroCampo));
    }

    private async void ConsultasHojeButton_Clicked(
        object sender,
        TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ConsultasHojeRecepcao));
    }
}