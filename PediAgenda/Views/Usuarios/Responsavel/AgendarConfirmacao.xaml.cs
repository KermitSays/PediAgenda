using System.Globalization;

namespace PediAgenda.Views.Usuarios.Responsavel;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(Paciente), "Paciente")]
[QueryProperty(nameof(MedicoId), "MedicoId")]
[QueryProperty(nameof(Medico), "Medico")]
[QueryProperty(nameof(Especialidade), "Especialidade")]
[QueryProperty(nameof(HorarioId), "HorarioId")]
[QueryProperty(nameof(Data), "Data")]
[QueryProperty(nameof(Horario), "Horario")]
[QueryProperty(nameof(TipoAtendimento), "TipoAtendimento")]
[QueryProperty(nameof(FotoMedico), "FotoMedico")]
public partial class AgendarConfirmacao : ContentPage
{
    private int pacienteId;

    private int medicoId;

    private int horarioId;

    private string? paciente;

    private string? medico;

    private string? especialidade;

    private string? data;

    private string? horario;

    private string? tipoAtendimento;

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

        set => paciente = value;
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


    public string TipoAtendimento
    {
        get => tipoAtendimento ?? string.Empty;

        set
        {
            tipoAtendimento =
                value;


            if (TipoAtendimentoLabel != null)
            {
                TipoAtendimentoLabel.Text =
                    value;
            }
        }
    }


    public AgendarConfirmacao()
    {
        InitializeComponent();
    }


    // CONFIRMAR AGENDAMENTO

    private async void ConfirmarButton_Clicked(
        object sender,
        EventArgs e)
    {
        if (agendamentoSalvo)
            return;


        PacienteResponsavelItem? pacienteEncontrado =
            ResponsavelDados.Pacientes
                .FirstOrDefault(p =>
                    p.Id == PacienteId);


        if (pacienteEncontrado == null)
        {
            await DisplayAlertAsync(
                "Erro",
                "Não foi possível localizar o paciente.",
                "OK");

            return;
        }


        if (!DateTime.TryParseExact(
            Data,
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime dataConsulta))
        {
            await DisplayAlertAsync(
                "Erro",
                "A data da consulta é inválida.",
                "OK");

            return;
        }


        if (!TimeSpan.TryParse(
            Horario,
            out TimeSpan horarioConsulta))
        {
            await DisplayAlertAsync(
                "Erro",
                "O horário da consulta é inválido.",
                "OK");

            return;
        }


        // Confere novamente antes de salvar.
        // Isso simula a validação que futuramente
        // será responsabilidade da API.

        bool horarioReservado =
            HorarioAgendamentoDados
                .ReservarHorario(
                    HorarioId);


        if (!horarioReservado)
        {
            await DisplayAlertAsync(
                "Horário indisponível",
                "Este horário não está mais disponível. " +
                "Selecione outro horário.",
                "OK");


            await Shell.Current.GoToAsync(
                "..");


            return;
        }


        int novoId =
            ResponsavelDados.Pacientes
                .SelectMany(p =>
                    p.Consultas)
                .Select(c =>
                    c.Id)
                .DefaultIfEmpty(0)
                .Max()
            + 1;


        string modalidade;

        string valor;


        if (TipoAtendimento.StartsWith(
            "Particular",
            StringComparison.OrdinalIgnoreCase))
        {
            modalidade =
                "Particular";

            valor =
                "R$ 200,00";
        }
        else
        {
            modalidade =
                "Convênio";

            valor =
                TipoAtendimento;
        }


        ConsultaResponsavelItem novaConsulta =
            new ConsultaResponsavelItem
            {
                Id =
                    novoId,

                IdMedico =
                    MedicoId,

                IdHorario =
                    HorarioId,

                Medico =
                    Medico,

                Especialidade =
                    Especialidade,

                Data =
                    dataConsulta,

                Horario =
                    horarioConsulta,

                Modalidade =
                    modalidade,

                Valor =
                    valor,

                Status =
                    "Por confirmar"
            };


        pacienteEncontrado.Consultas.Add(
            novaConsulta);


        NotificacoesDados.Notificacoes.Add(
            new Notificacao
            {
                Titulo =
                    "Consulta agendada",

                Mensagem =
                    $"A consulta de {Paciente} com {Medico} " +
                    $"foi agendada para " +
                    $"{dataConsulta:dd/MM/yyyy} às " +
                    $"{horarioConsulta:hh\\:mm}.",

                DataHora =
                    DateTime.Now,

                Lida =
                    false
            });


        agendamentoSalvo =
            true;


        await Shell.Current.GoToAsync(
            nameof(AgendamentoConcluido),
            new Dictionary<string, object>
            {
                {
                    "Paciente",
                    Paciente
                },

                {
                    "Medico",
                    Medico
                },

                {
                    "FotoMedico",
                    FotoMedico
                },

                {
                    "Especialidade",
                    Especialidade
                },

                {
                    "Data",
                    Data
                },

                {
                    "Horario",
                    Horario
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