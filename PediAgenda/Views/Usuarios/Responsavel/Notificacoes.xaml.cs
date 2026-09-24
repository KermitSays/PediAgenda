using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Responsavel;

public partial class Notificacoes : ContentPage
{
    public ObservableCollection<Notificacao> NotificacoesLista { get; set; }

    public Notificacoes()
    {
        InitializeComponent();

        // Dados mockados
        NotificacoesLista = new ObservableCollection<Notificacao>
        {
            new Notificacao
            {
                Titulo = "Consulta remarcada",
                Mensagem = "A clínica remarcou sua consulta com Dra. Ana Oliveira para 18/09/2026 às 15:00.",
                DataHora = new DateTime(2026, 9, 16, 10, 32, 0)
            },

            new Notificacao
            {
                Titulo = "Consulta cancelada",
                Mensagem = "Sua consulta com Dr. Carlos Mendes do dia 20/09/2026 às 14:00 foi cancelada pela clínica.",
                DataHora = new DateTime(2026, 9, 15, 8, 17, 0)
            },

            new Notificacao
            {
                Titulo = "Lembrete de consulta",
                Mensagem = "Você possui uma consulta amanhã às 09:30 com Dra. Ana Oliveira.",
                DataHora = new DateTime(2026, 9, 14, 9, 0, 0)
            },

            new Notificacao
            {
                Titulo = "Consulta confirmada",
                Mensagem = "Sua consulta com Dra. Juliana Santos foi confirmada para o dia 22/09/2026 às 10:00.",
                DataHora = new DateTime(2026, 9, 12, 14, 45, 0)
            },

            new Notificacao
            {
                Titulo = "Novo aviso da clínica",
                Mensagem = "A clínica informou que haverá alteração no horário de atendimento no dia 25/09/2026.",
                DataHora = new DateTime(2026, 9, 10, 16, 20, 0)
            }
        };

        BindingContext = this;
    }

    // Modelo da notificação
    public class Notificacao
    {
        public string Titulo { get; set; } = string.Empty;

        public string Mensagem { get; set; } = string.Empty;

        public DateTime DataHora { get; set; }

        public string DataHoraFormatada =>
            DataHora.ToString("dd/MM/yyyy 'às' HH:mm");
    }
}