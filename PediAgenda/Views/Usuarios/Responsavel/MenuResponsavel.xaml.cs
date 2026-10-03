namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class MenuResponsavel : ContentPage
{
    public MenuResponsavel()
    {
        InitializeComponent();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        CarregarProximaConsulta();
    }


    // CARREGA A PRÓXIMA CONSULTA

    private void CarregarProximaConsulta()
    {
        var proxima =
            ResponsavelDados.Pacientes
                .SelectMany(
                    paciente =>
                        paciente.Consultas.Select(
                            consulta => new
                            {
                                Paciente = paciente,
                                Consulta = consulta
                            }))
                .Where(item =>
                    item.Consulta.Data.Date >=
                        DateTime.Today

                    &&

                    !item.Consulta.Status.Equals(
                        "Cancelada",
                        StringComparison.OrdinalIgnoreCase)

                    &&

                    !item.Consulta.Status.Equals(
                        "Realizada",
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(item =>
                    item.Consulta.Data)
                .ThenBy(item =>
                    item.Consulta.Horario)
                .FirstOrDefault();


        if (proxima == null)
        {
            ProximaConsultaCard.IsVisible =
                false;

            SemConsultaCard.IsVisible =
                true;

            return;
        }


        ProximaConsultaCard.IsVisible =
            true;

        SemConsultaCard.IsVisible =
            false;


        ProximoMedicoLabel.Text =
            proxima.Consulta.Medico;


        ProximaEspecialidadeLabel.Text =
            proxima.Consulta.Especialidade;


        ProximoPacienteLabel.Text =
            proxima.Paciente.Nome;


        ProximaDataLabel.Text =
            proxima.Consulta.DataFormatada;


        ProximoHorarioLabel.Text =
            proxima.Consulta.HorarioFormatado;


        ProximaModalidadeLabel.Text =
            proxima.Consulta.Modalidade;


        ProximoStatusLabel.Text =
            proxima.Consulta.Status;
    }


    // PACIENTES

    private async void PacientesButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(PacientesResponsavel));
    }


    // NOTIFICAÇÕES

    private async void NotificacoesButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(Notificacoes));
    }


    // MINHAS CONSULTAS

    private async void MinhasConsultasButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(MinhasConsultas));
    }


    // AGENDAR CONSULTA

    private async void AgendarConsultaButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            nameof(AgendarConsulta));
    }
}