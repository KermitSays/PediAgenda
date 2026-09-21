namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class AgendarConsulta : ContentPage
{
    private string? especialidadeSelecionada;

    public AgendarConsulta()
    {
        InitializeComponent();
    }

    //Mockup de especialidades, futuramente será substituído por uma chamada à API
    private void EspecialidadePicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (EspecialidadePicker.SelectedIndex >= 0)
        {
            especialidadeSelecionada =
                EspecialidadePicker.Items[EspecialidadePicker.SelectedIndex];

            ErroLabel.IsVisible = false;
        }
        else
        {
            especialidadeSelecionada = null;
        }
    }

    // Botão para continuar para a próxima tela
    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (string.IsNullOrEmpty(especialidadeSelecionada))
        {
            ErroLabel.Text =
                "Selecione uma especialidade para continuar.";

            ErroLabel.IsVisible = true;
            return;
        }

        // Envia a especialidade escolhida para a próxima tela, Pode ser substituído futuramente
        // por uma chamada à API para buscar os médicos disponíveis
        await Shell.Current.GoToAsync(
            nameof(AgendarMedico),
            new Dictionary<string, object>
            {
                { "Especialidade", especialidadeSelecionada }
            });
    }

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
