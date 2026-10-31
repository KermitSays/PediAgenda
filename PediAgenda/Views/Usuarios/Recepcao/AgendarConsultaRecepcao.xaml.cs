using System.Collections.ObjectModel;
using System.Globalization;

namespace PediAgenda.Views.Usuarios.Recepcao;

[QueryProperty(nameof(PacienteId), "PacienteId")]
[QueryProperty(nameof(HorarioId), "HorarioId")]
[QueryProperty(nameof(PacienteDaApi), "PacienteDaApi")]
public partial class AgendarConsultaRecepcao : ContentPage
{
    private int idPaciente;
    private PacienteRecepcaoItem? paciente;
    private PacienteRecepcaoItem? pacienteDaApi;
    private readonly List<MedicoRecepcaoResposta> medicos = new();
    private HorarioAgendaRecepcao? horarioSelecionado;
    private bool carregando;
    private bool concluido;
    private bool reagendamentoSolicitado;
    private bool ajustandoData;
    private int versaoHorarios;

    public ObservableCollection<HorarioAgendaRecepcao> HorariosDisponiveis { get; } = new();

    public string PacienteId
    {
        set { if (int.TryParse(value, out int id) && id > 0) idPaciente = id; }
    }

    public PacienteRecepcaoItem? PacienteDaApi
    {
        get => pacienteDaApi;
        set
        {
            pacienteDaApi = value;
            if (value is not null) idPaciente = value.Id;
        }
    }

    // Mantém compatibilidade com a rota antiga sem reintroduzir reagendamento.
    public string HorarioId
    {
        set { if (!string.IsNullOrWhiteSpace(value)) reagendamentoSolicitado = true; }
    }

    public AgendarConsultaRecepcao()
    {
        InitializeComponent();
        BindingContext = this;
        var primeira = ProximoDiaUtil(DateTime.Today);
        DataPicker.MinimumDate = primeira;
        DataPicker.Date = primeira;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (concluido || carregando) return;
        if (reagendamentoSolicitado)
        {
            await DisplayAlertAsync("Reagendamento indisponível",
                "Para alterar uma consulta, cancele-a e faça um novo agendamento.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        carregando = true;
        try
        {
            await CarregarPacienteAsync();
            await CarregarMedicosAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Erro ao carregar agendamento", ex.Message, "OK");
        }
        finally { carregando = false; }
        await CarregarHorariosAsync();
    }

    private static bool EhDiaUtil(DateTime data) =>
        data.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    private static DateTime ProximoDiaUtil(DateTime data)
    {
        var dia = data.Date;
        while (!EhDiaUtil(dia)) dia = dia.AddDays(1);
        return dia;
    }

    private async Task CarregarPacienteAsync()
    {
        // Mesmo quando o paciente chega da tela de detalhes, sua existência
        // é conferida na lista autorizada da API, nunca na lista local.
        var lista = await PacientesRecepcaoApi.ListarAsync();
        var encontrado = lista.FirstOrDefault(p => p.Id == idPaciente);
        if (encontrado is null)
            throw new InvalidOperationException("Paciente não localizado no banco de dados.");

        paciente = new PacienteRecepcaoItem
        {
            Id = encontrado.Id,
            Nome = encontrado.Nome,
            Responsavel = encontrado.Responsavel,
            TelefoneResponsavel = encontrado.TelefoneResponsavel,
            EmailResponsavel = encontrado.EmailResponsavel ?? string.Empty,
            DataNascimento = DateTime.ParseExact(encontrado.DataNascimento,
                "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Status = "Cadastrado"
        };
        PacienteLabel.Text = paciente.Nome;
        ResponsavelLabel.Text = $"Responsável: {paciente.Responsavel}";
    }

    private async Task CarregarMedicosAsync()
    {
        MedicoPicker.SelectedIndex = -1;
        MedicoPicker.Items.Clear();
        medicos.Clear();
        var lista = await AgendaRecepcaoApi.ListarMedicosAsync();
        medicos.AddRange(lista.OrderBy(m => m.Nome));
        foreach (var medico in medicos) MedicoPicker.Items.Add(medico.Nome);
        if (medicos.Count > 0) MedicoPicker.SelectedIndex = 0;
    }

    private async void MedicoPicker_SelectedIndexChanged(object sender, EventArgs e) =>
        await CarregarHorariosAsync();

    private async void DataPicker_DateSelected(object sender, DateChangedEventArgs e)
    {
        if (ajustandoData) return;
        DateTime data = e.NewDate ?? DateTime.Today;
        if (!EhDiaUtil(data))
        {
            await DisplayAlertAsync("Data indisponível",
                "A clínica não realiza atendimentos aos sábados ou domingos.", "OK");
            ajustandoData = true;
            DataPicker.Date = ProximoDiaUtil(data);
            ajustandoData = false;
        }
        await CarregarHorariosAsync();
    }

    private async Task CarregarHorariosAsync()
    {
        int versao = ++versaoHorarios;
        HorariosDisponiveis.Clear();
        horarioSelecionado = null;
        HorarioSelecionadoLabel.Text = "Nenhum";
        int indice = MedicoPicker.SelectedIndex;
        if (carregando || indice < 0 || indice >= medicos.Count) return;

        DateTime dia = (DataPicker.Date ?? DateTime.Today).Date;
        if (!EhDiaUtil(dia)) return;

        try
        {
            var medico = medicos[indice];
            var agenda = await AgendaRecepcaoApi.ConsultarAsync(dia, medico.IdMedico);
            if (versao != versaoHorarios) return;

            foreach (var item in agenda.Medicos)
            {
                foreach (var horario in item.Horarios.Where(h => h.Situacao == "LIVRE"))
                {
                    if (!TimeSpan.TryParseExact(horario.Inicio, @"hh\:mm",
                        CultureInfo.InvariantCulture, out var hora)) continue;
                    if (dia.Add(hora) <= DateTime.Now) continue;
                    HorariosDisponiveis.Add(new HorarioAgendaRecepcao
                    {
                        Id = horario.Id,
                        IdMedico = item.Id,
                        Medico = item.Nome,
                        Data = dia,
                        Horario = hora,
                        Status = "Disponível"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            if (versao != versaoHorarios) return;
            await DisplayAlertAsync("Erro ao carregar horários", ex.Message, "OK");
        }
    }

    private void Horario_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        horarioSelecionado = e.CurrentSelection.FirstOrDefault() as HorarioAgendaRecepcao;
        HorarioSelecionadoLabel.Text = horarioSelecionado?.HorarioFormatado ?? "Nenhum";
    }

    private async void ConfirmarButton_Clicked(object sender, EventArgs e)
    {
        if (concluido || carregando) return;
        if (paciente is null || horarioSelecionado is null)
        {
            await DisplayAlertAsync("Atenção", "Selecione o paciente e um horário.", "OK");
            return;
        }
        var horario = horarioSelecionado;
        bool confirmar = await DisplayAlertAsync("Confirmar agendamento",
            $"Paciente: {paciente.Nome}\nResponsável: {paciente.Responsavel}\n" +
            $"Médico: {horario.Medico}\nData: {horario.Data:dd/MM/yyyy}\n" +
            $"Horário: {horario.HorarioFormatado}", "CONFIRMAR", "VOLTAR");
        if (!confirmar) return;

        carregando = true;
        ConfirmarButton.IsEnabled = false;
        try
        {
            await AgendamentoRecepcaoApi.AgendarAsync(paciente.Id, horario.Id);
            concluido = true;
            await DisplayAlertAsync("Consulta agendada",
                "A consulta foi registrada no banco de dados.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Não foi possível agendar", ex.Message, "OK");
            carregando = false;
            ConfirmarButton.IsEnabled = true;
            await CarregarHorariosAsync();
        }
    }

    private async void VoltarButton_Clicked(object sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
