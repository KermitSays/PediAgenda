namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(HorarioId), "HorarioId")]
public partial class DetalhesConsultaRecepcao : ContentPage
{
    private int _horarioId;

    private HorarioAgendaRecepcao? _horario;


    public string HorarioId
    {
        set
        {
            if (int.TryParse(value, out int id))
            {
                _horarioId = id;

                CarregarConsulta();
            }
        }
    }


    public DetalhesConsultaRecepcao()
    {
        InitializeComponent();
    }


    protected override void OnAppearing()
    {
        base.OnAppearing();

        CarregarConsulta();
    }


    private void CarregarConsulta()
    {
        if (_horarioId == 0)
            return;


        _horario =
            AgendaMedicaRecepcaoDados.Horarios
                .FirstOrDefault(h => h.Id == _horarioId);


        if (_horario == null)
            return;


        PacienteLabel.Text =
            _horario.Paciente;


        MedicoLabel.Text =
            _horario.Medico;


        DataLabel.Text =
            _horario.Data.ToString("dd/MM/yyyy");


        HorarioLabel.Text =
            _horario.HorarioFormatado;


        // Procura a consulta correspondente no histórico do paciente
        var paciente =
            PacientesRecepcaoDados.Pacientes
                .FirstOrDefault(p =>
                    p.Nome.Equals(
                        _horario.Paciente,
                        StringComparison.OrdinalIgnoreCase));


        if (paciente != null)
        {
            ResponsavelLabel.Text =
                $"Responsável: {paciente.Responsavel}";


            var consulta =
                paciente.Consultas
                    .FirstOrDefault(c =>
                        c.HorarioId == _horario.Id);


            if (consulta != null)
            {
                StatusLabel.Text =
                    consulta.Status;
            }
            else
            {
                StatusLabel.Text =
                    "Agendado";
            }
        }
        else
        {
            ResponsavelLabel.Text =
                "Responsável não localizado";

            StatusLabel.Text =
                "Agendado";
        }


        AtualizarBotoes();
    }


    private void AtualizarBotoes()
    {
        if (_horario == null)
            return;


        // Se o horário está agendado,
        // a consulta ainda pode ser gerenciada.
        bool consultaAtiva =
            _horario.Status == "Agendado";


        ConfirmarButton.IsEnabled =
            consultaAtiva;

        ReagendarButton.IsEnabled =
            consultaAtiva;

        CancelarButton.IsEnabled =
            consultaAtiva;


        // Caso a consulta já esteja confirmada,
        // não precisa permitir confirmar novamente.
        if (StatusLabel.Text == "Confirmado")
        {
            ConfirmarButton.IsEnabled = false;

            ConfirmarButton.Text =
                "CONSULTA CONFIRMADA";
        }
        else
        {
            ConfirmarButton.Text =
                "CONFIRMAR CONSULTA";
        }
    }


    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_horario == null)
            return;


        bool confirmar =
            await DisplayAlertAsync(
                "Confirmar consulta",
                $"Confirmar a consulta de {_horario.Paciente}?",
                "CONFIRMAR",
                "VOLTAR");


        if (!confirmar)
            return;


        // Procura o paciente relacionado ao horário
        var paciente =
            PacientesRecepcaoDados.Pacientes
                .FirstOrDefault(p =>
                    p.Nome.Equals(
                        _horario.Paciente,
                        StringComparison.OrdinalIgnoreCase));


        if (paciente != null)
        {
            // Localiza a mesma consulta no histórico do paciente
            var consulta =
                paciente.Consultas
                    .FirstOrDefault(c =>
                        c.HorarioId == _horario.Id);


            if (consulta != null)
            {
                consulta.Status =
                    "Confirmado";
            }
        }


        StatusLabel.Text =
            "Confirmado";


        AtualizarBotoes();


        await DisplayAlertAsync(
            "Consulta confirmada",
            "A consulta foi confirmada com sucesso.",
            "OK");
    }


    private async void ReagendarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_horario == null)
            return;


        var paciente =
            PacientesRecepcaoDados.Pacientes
                .FirstOrDefault(p =>
                    p.Nome.Equals(
                        _horario.Paciente,
                        StringComparison.OrdinalIgnoreCase));


        if (paciente == null)
        {
            await DisplayAlertAsync(
                "Paciente não encontrado",
                "Não foi possível localizar o paciente.",
                "OK");

            return;
        }


        await Shell.Current.GoToAsync(
            $"{nameof(AgendarConsultaRecepcao)}" +
            $"?PacienteId={paciente.Id}" +
            $"&HorarioId={_horario.Id}");
    }


    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (_horario == null)
            return;


        bool cancelar =
            await DisplayAlertAsync(
                "Cancelar consulta",
                $"Deseja cancelar a consulta de {_horario.Paciente} " +
                $"no dia {_horario.Data:dd/MM/yyyy} às {_horario.HorarioFormatado}?",
                "CANCELAR CONSULTA",
                "VOLTAR");


        if (!cancelar)
            return;


        // Guarda o nome antes de limpar o horário
        string nomePaciente =
            _horario.Paciente;


        // Procura o paciente relacionado à consulta
        var pacienteEncontrado =
            PacientesRecepcaoDados.Pacientes
                .FirstOrDefault(p =>
                    p.Nome.Equals(
                        nomePaciente,
                        StringComparison.OrdinalIgnoreCase));


        if (pacienteEncontrado != null)
        {
            // Procura a consulta no histórico
            var consulta =
                pacienteEncontrado.Consultas
                    .FirstOrDefault(c =>
                        c.HorarioId == _horario.Id);


            if (consulta != null)
            {
                // Mantém a consulta no histórico,
                // mas marca como cancelada
                consulta.Status =
                    "Cancelado";

                // Como o horário foi liberado,
                // a consulta não possui mais horário ativo
                consulta.HorarioId =
                    null;
            }
        }


        // Libera o horário da agenda médica
        _horario.Status =
            "Disponível";

        _horario.Paciente =
            string.Empty;


        await DisplayAlertAsync(
            "Consulta cancelada",
            $"A consulta de {nomePaciente} foi cancelada " +
            "e o horário foi liberado.",
            "OK");


        await Shell.Current.GoToAsync("..");
    }


    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}