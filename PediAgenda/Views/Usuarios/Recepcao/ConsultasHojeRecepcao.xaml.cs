using System.Collections.ObjectModel;
using PediAgenda.Dados;

namespace PediAgenda.Views.Usuarios.Recepcao;

public partial class ConsultasHojeRecepcao : ContentPage
{
    public ObservableCollection<ConsultaRecepcaoResumo> ConsultasHoje { get; } = new();

    // Evita exibir resultados antigos se a tela for aberta rapidamente mais de uma vez.
    private int _versaoCarregamento;

    public ConsultasHojeRecepcao()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarConsultasDoDiaAsync();
    }

    private async Task CarregarConsultasDoDiaAsync()
    {
        int versaoAtual = ++_versaoCarregamento;
        DateTime hoje = DateTime.Today;

        ConsultasHoje.Clear();
        ResumoLabel.Text = $"{hoje:dd/MM/yyyy} • Carregando consultas...";

        try
        {
            // Mesma rota real ja utilizada no Menu Recepcao: GET /api/agenda.
            // O perfil Recepcao tem permissao para consultar todos os medicos.
            var agenda = await AgendaRecepcaoApi.ConsultarAsync(hoje);

            if (versaoAtual != _versaoCarregamento)
                return;

            var consultas = new List<ConsultaRecepcaoResumo>();

            foreach (var medico in agenda.Medicos)
            {
                foreach (var horario in medico.Horarios)
                {
                    var consulta = horario.Consulta;

                    // A agenda ativa nao deve mostrar cancelamentos antigos.
                    // Eles continuam registrados no Aiven para o historico.
                    if (consulta is null || StatusConsulta.EhCancelada(consulta.Status))
                        continue;

                    consultas.Add(new ConsultaRecepcaoResumo
                    {
                        Horario = horario.Inicio,
                        Paciente = consulta.Paciente?.Nome ?? "Paciente nao informado",
                        Medico = medico.Nome,
                        Status = consulta.Status
                    });
                }
            }

            foreach (var consulta in consultas
                         .OrderBy(c => c.Horario)
                         .ThenBy(c => c.Medico))
            {
                ConsultasHoje.Add(consulta);
            }

            ResumoLabel.Text =
                $"{hoje:dd/MM/yyyy} • {ConsultasHoje.Count} " +
                (ConsultasHoje.Count == 1 ? "consulta" : "consultas");
        }
        catch (Exception ex)
        {
            if (versaoAtual != _versaoCarregamento)
                return;

            System.Diagnostics.Debug.WriteLine(ex);
            ResumoLabel.Text = $"{hoje:dd/MM/yyyy} • Erro ao carregar consultas";

            await DisplayAlertAsync(
                "Erro ao carregar consultas",
                "Nao foi possivel consultar a agenda pela API. " +
                "Verifique a conexao e o login da Recepcao.",
                "OK");
        }
    }

    private async void VoltarButton_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

// Compartilhado com o MenuRecepcao, que ja usa a agenda real da API.
public class ConsultaRecepcaoResumo
{
    public string Horario { get; set; } = string.Empty;
    public string Paciente { get; set; } = string.Empty;
    public string Medico { get; set; } = string.Empty;

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set => _status = StatusConsulta.Normalizar(value);
    }
}
