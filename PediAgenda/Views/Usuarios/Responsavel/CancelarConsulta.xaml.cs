namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(ConsultaId), "ConsultaId")]
[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
public partial class CancelarConsulta : ContentPage
{
    private int consultaId;

    private string? medico;

    private string? especialidade;

    private string? paciente;

    private string? data;

    private string? horario;


    public int ConsultaId
    {
        get => consultaId;

        set => consultaId = value;
    }


    public string Medico
    {
        get => medico ?? string.Empty;

        set
        {
            medico = value;

            if (MedicoLabel != null)
            {
                MedicoLabel.Text =
                    value;
            }
        }
    }


    public string Especialidade
    {
        get => especialidade ?? string.Empty;

        set
        {
            especialidade = value;

            if (EspecialidadeLabel != null)
            {
                EspecialidadeLabel.Text =
                    value;
            }
        }
    }


    public string Paciente
    {
        get => paciente ?? string.Empty;

        set
        {
            paciente = value;

            if (PacienteLabel != null)
            {
                PacienteLabel.Text =
                    value;
            }
        }
    }


    public string Data
    {
        get => data ?? string.Empty;

        set
        {
            data = value;

            if (DataLabel != null)
            {
                DataLabel.Text =
                    value;
            }
        }
    }


    public string Horario
    {
        get => horario ?? string.Empty;

        set
        {
            horario = value;

            if (HorarioLabel != null)
            {
                HorarioLabel.Text =
                    value;
            }
        }
    }


    public CancelarConsulta()
    {
        InitializeComponent();
    }


    // CONFIRMAR CANCELAMENTO
    private async void CancelarButton_Clicked(
        object sender,
        EventArgs e)
    {
        bool confirmar =
            await DisplayAlertAsync(
                "Cancelar consulta",
                "Deseja realmente cancelar esta consulta?",
                "SIM",
                "NÃO");


        if (!confirmar)
            return;


        // Localiza o paciente
        PacienteResponsavelItem? pacienteEncontrado =
            ResponsavelDados.Pacientes
                .FirstOrDefault(p =>
                    p.Nome.Equals(
                        Paciente,
                        StringComparison.OrdinalIgnoreCase));


        if (pacienteEncontrado == null)
        {
            await DisplayAlertAsync(
                "Erro",
                "Não foi possível localizar o paciente.",
                "OK");

            return;
        }


        // Localiza a consulta pelo ID
        ConsultaResponsavelItem? consulta =
            pacienteEncontrado.Consultas
                .FirstOrDefault(c =>
                    c.Id == ConsultaId);


        if (consulta == null)
        {
            await DisplayAlertAsync(
                "Erro",
                "Não foi possível localizar a consulta.",
                "OK");

            return;
        }


        if (consulta.Status.Equals(
            "Cancelada",
            StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlertAsync(
                "Consulta cancelada",
                "Esta consulta já está cancelada.",
                "OK");

            return;
        }


        // Mantém no histórico
        consulta.Status =
            "Cancelada";


        // Cria a notificação
        NotificacoesDados.Notificacoes.Add(
            new Notificacao
            {
                Titulo =
                    "Consulta cancelada",

                Mensagem =
                    $"A consulta de {Paciente} com {Medico} " +
                    $"do dia {consulta.Data:dd/MM/yyyy} às " +
                    $"{consulta.Horario:hh\\:mm} foi cancelada.",

                DataHora =
                    DateTime.Now,

                Lida =
                    false
            });


        await DisplayAlertAsync(
            "Consulta cancelada",
            "A consulta foi cancelada com sucesso.",
            "OK");


        await Shell.Current.GoToAsync(
            nameof(MinhasConsultas));
    }


    // VOLTAR
    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}