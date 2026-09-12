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

        private async void OnResponsavelClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginResponsavel(false));
        }

        private async void OnFuncionarioClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginFuncionario(true));
        }
    }
}
