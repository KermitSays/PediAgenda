namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class AgendarConsulta : ContentPage
{
    private PacienteResponsavelItem? pacienteSelecionado;

    private string? especialidadeSelecionada;


    public AgendarConsulta()
    {
        InitializeComponent();

        CarregarPacientes();
    }


    // CARREGA OS DEPENDENTES DO RESPONSÁVEL

    private void CarregarPacientes()
    {
        PacientePicker.Items.Clear();


        foreach (PacienteResponsavelItem paciente
            in ResponsavelDados.Pacientes)
        {
            PacientePicker.Items.Add(
                paciente.Nome);
        }
    }


    // PACIENTE SELECIONADO

    private void PacientePicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (PacientePicker.SelectedIndex < 0)
        {
            pacienteSelecionado = null;

            return;
        }


        pacienteSelecionado =
            ResponsavelDados.Pacientes[
                PacientePicker.SelectedIndex];


        ErroLabel.IsVisible =
            false;
    }


    // ESPECIALIDADE SELECIONADA

    private void EspecialidadePicker_SelectedIndexChanged(
        object sender,
        EventArgs e)
    {
        if (EspecialidadePicker.SelectedIndex >= 0)
        {
            especialidadeSelecionada =
                EspecialidadePicker.Items[
                    EspecialidadePicker.SelectedIndex];


            ErroLabel.IsVisible =
                false;
        }
        else
        {
            especialidadeSelecionada =
                null;
        }
    }


    // CONTINUAR

    private async void ContinuarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (pacienteSelecionado == null)
        {
            ErroLabel.Text =
                "Selecione o paciente para continuar.";

            ErroLabel.IsVisible =
                true;

            return;
        }


        if (string.IsNullOrEmpty(
            especialidadeSelecionada))
        {
            ErroLabel.Text =
                "Selecione uma especialidade para continuar.";

            ErroLabel.IsVisible =
                true;

            return;
        }


        await Shell.Current.GoToAsync(
            nameof(AgendarMedico),
            new Dictionary<string, object>
            {
                {
                    "PacienteId",
                    pacienteSelecionado.Id
                },

                {
                    "Paciente",
                    pacienteSelecionado.Nome
                },

                {
                    "Especialidade",
                    especialidadeSelecionada
                }
            });
    }


    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}