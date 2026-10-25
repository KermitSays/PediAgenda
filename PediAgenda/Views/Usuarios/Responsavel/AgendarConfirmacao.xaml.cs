using PediAgenda.Dados;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(MedicoId), "MedicoId")]
[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(HorarioId), "HorarioId")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
public partial class AgendarConfirmacao : ContentPage
{
    private static readonly HttpClient http = new()
    {
         BaseAddress = new Uri("http://10.0.2.2:5000/"),
         Timeout = TimeSpan.FromSeconds(90)
    };
    private int pacienteId;

    private int medicoId;

    private int horarioId;

    private string? paciente;

    private string? medico;

    private string? especialidade;

    private string? data;

    private string? horario;
    private string? fotoMedico;

    private bool agendamentoSalvo;


    public int PacienteId
    {
        get => pacienteId;

        set => pacienteId = value;
    }


    public int MedicoId
    {
        get => medicoId;

        set => medicoId = value;
    }


    public int HorarioId
    {
        get => horarioId;

        set => horarioId = value;
    }


    public string Paciente
    {
        get => paciente ?? string.Empty;

        set
        {
            paciente = value;
            if (PacienteLabel != null)
                PacienteLabel.Text = value;
        }
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


    public string FotoMedico
    {
        get => fotoMedico ?? string.Empty;

        set => fotoMedico = value;
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


    public AgendarConfirmacao()
    {
        InitializeComponent();

        ResponsavelLabel.Text = SessaoUsuario.Nome;
    }


    // CONFIRMAR AGENDAMENTO

    private async void ConfirmarButton_Clicked(object sender, EventArgs e)
    {
        if (agendamentoSalvo)
            return;

        if (PacienteId <= 0 || HorarioId <= 0)
        {
            await DisplayAlertAsync(
                "Erro nos dados",
                $"PacienteId: {PacienteId}\nHorarioId: {HorarioId}",
                "OK");
            return;
        }

        try
        {
            agendamentoSalvo = true;

            string? token = await SecureStorage.Default
                .GetAsync("pediagenda_token");

            if (string.IsNullOrWhiteSpace(token))
            {
                await DisplayAlertAsync(
                    "Acesso não autorizado",
                    "Faça login novamente para agendar a consulta.",
                    "OK");
                return;
            }

            var dados = new
            {
                idPaciente = PacienteId,
                idHorario = HorarioId,
                tipoAtendimento = (string?)null
            };

            using var requisicao = new HttpRequestMessage(
                HttpMethod.Post,
                "api/consultas");

            requisicao.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            requisicao.Content = JsonContent.Create(dados);

            using var resposta = await http.SendAsync(requisicao);

            if (!resposta.IsSuccessStatusCode)
            {
                await DisplayAlertAsync(
                    "Não foi possível agendar",
                    "Verifique se o horário continua disponível e tente novamente.",
                    "OK");

                return;
            }

            await Shell.Current.GoToAsync(
                nameof(AgendamentoConcluido),
                new Dictionary<string, object>
                {
                { "Responsavel", SessaoUsuario.Nome },
                { "Paciente", Paciente },
                { "Medico", Medico },
                { "FotoMedico", FotoMedico },
                { "Especialidade", Especialidade },
                { "Data", Data },
                { "Horario", Horario }
                });
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Erro de conexão",
                $"Não foi possível concluir o agendamento: {ex.Message}",
                "OK");
        }
        finally
        {
            agendamentoSalvo = false;
        }
    }


    // VOLTAR

    private async void VoltarButton_Clicked(
        object sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}