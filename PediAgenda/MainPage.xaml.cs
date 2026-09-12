using PediAgenda.Views;


namespace PediAgenda
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnResponsavelClicked(
            object sender, 
            EventArgs e)
        {
            await Shell.Current.GoToAsync(
            nameof(LoginResponsavel));
        }

        private async void OnFuncionarioClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(
            nameof(LoginFuncionario));
        }
    }
}
