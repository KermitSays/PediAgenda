namespace PediAgenda.Views.Usuarios.Recepcao;

public static class PacientesRecepcaoDados
{
    public static List<PacienteRecepcaoItem> Pacientes { get; } = new()
    {
        new PacienteRecepcaoItem
        {
            Id = 1,
            Nome = "Maria Alice",
            DataNascimento = new DateTime(2021, 3, 14),
            Responsavel = "Ana Oliveira",
            TelefoneResponsavel = "(11) 99999-1111",
            EmailResponsavel = "ana.oliveira@email.com",
            Status = "Ativo",

            Consultas = new List<ConsultaPacienteRecepcaoItem>
            {
                new ConsultaPacienteRecepcaoItem
                {
                    Data = new DateTime(2026, 10, 5),
                    Horario = new TimeSpan(9, 0, 0),
                    Medico = "Dr. Carlos Mendes",
                    Especialidade = "Pediatria Geral",
                    TipoAtendimento = "Particular",
                    Status = "Confirmado"
                },

                new ConsultaPacienteRecepcaoItem
                {
                    Data = new DateTime(2026, 8, 18),
                    Horario = new TimeSpan(10, 0, 0),
                    Medico = "Dra. Fernanda Lima",
                    Especialidade = "Pediatria Geral",
                    TipoAtendimento = "Convênio",
                    Status = "Realizado"
                }
            }
        },

        new PacienteRecepcaoItem
        {
            Id = 2,
            Nome = "João Pedro",
            DataNascimento = new DateTime(2020, 9, 8),
            Responsavel = "Carla Souza",
            TelefoneResponsavel = "(11) 98888-2222",
            EmailResponsavel = "carla.souza@email.com",
            Status = "Ativo",

            Consultas = new List<ConsultaPacienteRecepcaoItem>
            {
                new ConsultaPacienteRecepcaoItem
                {
                    Data = new DateTime(2026, 10, 6),
                    Horario = new TimeSpan(14, 0, 0),
                    Medico = "Dra. Fernanda Lima",
                    Especialidade = "Pediatria Geral",
                    TipoAtendimento = "Convênio",
                    Status = "Por Confirmar"
                }
            }
        },

        new PacienteRecepcaoItem
        {
            Id = 3,
            Nome = "Bianca",
            DataNascimento = new DateTime(2019, 6, 22),
            Responsavel = "Mariana Santos",
            TelefoneResponsavel = "(11) 97777-3333",
            EmailResponsavel = "mariana.santos@email.com",
            Status = "Ativo",

            Consultas = new List<ConsultaPacienteRecepcaoItem>
            {
                new ConsultaPacienteRecepcaoItem
                {
                    Data = new DateTime(2026, 10, 8),
                    Horario = new TimeSpan(15, 0, 0),
                    Medico = "Dr. Carlos Mendes",
                    Especialidade = "Pediatria Geral",
                    TipoAtendimento = "Particular",
                    Status = "Confirmado"
                }
            }
        },

        new PacienteRecepcaoItem
        {
            Id = 4,
            Nome = "Isaac",
            DataNascimento = new DateTime(2022, 1, 11),
            Responsavel = "Fernanda Lima",
            TelefoneResponsavel = "(11) 96666-4444",
            EmailResponsavel = "fernanda.lima@email.com",
            Status = "Ativo",

            Consultas = new List<ConsultaPacienteRecepcaoItem>
            {
                new ConsultaPacienteRecepcaoItem
                {
                    Data = new DateTime(2026, 9, 30),
                    Horario = new TimeSpan(12, 0, 0),
                    Medico = "Dr. Carlos Mendes",
                    Especialidade = "Pediatria Geral",
                    TipoAtendimento = "Particular",
                    Status = "Cancelado"
                }
            }
        }
    };
}