namespace PediAgenda.Views.Usuarios.Responsavel;

public static class NotificacoesDados
{
    public static List<Notificacao>
        Notificacoes
    { get; } =
        new()
        {
            new Notificacao
            {
                Titulo =
                    "Consulta remarcada",

                Mensagem =
                    "A clínica remarcou sua consulta com " +
                    "Dra. Ana Oliveira para 18/09/2026 às 15:00.",

                DataHora =
                    new DateTime(
                        2026,
                        9,
                        16,
                        10,
                        32,
                        0),

                Lida =
                    true
            },


            new Notificacao
            {
                Titulo =
                    "Consulta cancelada",

                Mensagem =
                    "Sua consulta com Dr. Carlos Mendes " +
                    "do dia 20/09/2026 às 14:00 foi cancelada pela clínica.",

                DataHora =
                    new DateTime(
                        2026,
                        9,
                        15,
                        8,
                        17,
                        0),

                Lida =
                    true
            },


            new Notificacao
            {
                Titulo =
                    "Lembrete de consulta",

                Mensagem =
                    "Você possui uma consulta amanhã às 09:30 " +
                    "com Dra. Ana Oliveira.",

                DataHora =
                    new DateTime(
                        2026,
                        9,
                        14,
                        9,
                        0,
                        0),

                Lida =
                    true
            },


            new Notificacao
            {
                Titulo =
                    "Consulta confirmada",

                Mensagem =
                    "Sua consulta com Dra. Juliana Santos foi confirmada " +
                    "para o dia 22/09/2026 às 10:00.",

                DataHora =
                    new DateTime(
                        2026,
                        9,
                        12,
                        14,
                        45,
                        0),

                Lida =
                    true
            },


            new Notificacao
            {
                Titulo =
                    "Novo aviso da clínica",

                Mensagem =
                    "A clínica informou que haverá alteração no horário " +
                    "de atendimento no dia 25/09/2026.",

                DataHora =
                    new DateTime(
                        2026,
                        9,
                        10,
                        16,
                        20,
                        0),

                Lida =
                    true
            }
        };
}