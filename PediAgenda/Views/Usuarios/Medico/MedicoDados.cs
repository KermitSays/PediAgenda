using System.Collections.ObjectModel;

namespace PediAgenda.Views.Usuarios.Medico;

public static class MedicoDados
{
    public static ObservableCollection<PacienteMedicoItem>
        Pacientes
    { get; } =
        new()
        {
            new PacienteMedicoItem
            {
                IdPaciente = 1,

                Nome =
                    "João da Silva",

                Foto =
                    "paciente1.png",

                DataNascimento =
                    new DateTime(
                        2018,
                        5,
                        12),

                Responsavel =
                    "Carlos da Silva",

                Historico =
                    new ObservableCollection<ConsultaHistoricoMedicoItem>
                    {
                        new ConsultaHistoricoMedicoItem
                        {
                            IdHistorico = 1,

                            TipoConsulta =
                                "Consulta pediátrica",

                            Data =
                                new DateTime(
                                    2026,
                                    6,
                                    2),

                            Observacoes =
                                "Paciente compareceu acompanhado do responsável. " +
                                "Relatado quadro de febre e coriza nos últimos dias. " +
                                "Realizada avaliação clínica durante a consulta. " +
                                "Orientações registradas conforme avaliação médica."
                        },

                        new ConsultaHistoricoMedicoItem
                        {
                            IdHistorico = 2,

                            TipoConsulta =
                                "Consulta pediátrica",

                            Data =
                                new DateTime(
                                    2026,
                                    4,
                                    5),

                            Observacoes =
                                "Consulta de acompanhamento. Responsável relata " +
                                "melhora dos sintomas apresentados anteriormente. " +
                                "Paciente avaliado durante a consulta e acompanhamento mantido."
                        }
                    }
            },


            new PacienteMedicoItem
            {
                IdPaciente = 2,

                Nome =
                    "Maria Alice",

                Foto =
                    "paciente2.png",

                DataNascimento =
                    new DateTime(
                        2020,
                        8,
                        25),

                Responsavel =
                    "Fernanda Alice",

                Historico =
                    new ObservableCollection<ConsultaHistoricoMedicoItem>
                    {
                        new ConsultaHistoricoMedicoItem
                        {
                            IdHistorico = 3,

                            TipoConsulta =
                                "Consulta pediátrica",

                            Data =
                                new DateTime(
                                    2026,
                                    5,
                                    18),

                            Observacoes =
                                "Paciente compareceu para avaliação de rotina " +
                                "acompanhada do responsável. Realizada avaliação clínica " +
                                "e registradas as orientações referentes à consulta."
                        }
                    }
            },


            new PacienteMedicoItem
            {
                IdPaciente = 3,

                Nome =
                    "Renata Oliveira",

                Foto =
                    "paciente3.png",

                DataNascimento =
                    new DateTime(
                        2017,
                        11,
                        3),

                Responsavel =
                    "Marcos Oliveira",

                Historico =
                    new ObservableCollection<ConsultaHistoricoMedicoItem>()
            }
        };


    public static PacienteMedicoItem?
        ObterPacientePorId(
            int idPaciente)
    {
        return Pacientes
            .FirstOrDefault(p =>
                p.IdPaciente ==
                idPaciente);
    }


    public static ConsultaHistoricoMedicoItem?
        ObterHistoricoPorId(
            int idHistorico)
    {
        return Pacientes
            .SelectMany(p =>
                p.Historico)
            .FirstOrDefault(h =>
                h.IdHistorico ==
                idHistorico);
    }


    public static int ProximoIdHistorico()
    {
        return Pacientes
            .SelectMany(p =>
                p.Historico)
            .Select(h =>
                h.IdHistorico)
            .DefaultIfEmpty(0)
            .Max()
            + 1;
    }
}


public class PacienteMedicoItem
{
    public int IdPaciente { get; set; }


    public string Nome { get; set; } =
        string.Empty;


    public string Foto { get; set; } =
        string.Empty;


    public DateTime DataNascimento
    {
        get;
        set;
    }


    public string Responsavel { get; set; } =
        string.Empty;


    public ObservableCollection<ConsultaHistoricoMedicoItem>
        Historico
    { get; set; } =
            new();


    public bool PrimeiraConsulta =>
        Historico.Count == 0;


    public DateTime? UltimaConsulta =>
        Historico
            .OrderByDescending(h =>
                h.Data)
            .Select(h =>
                (DateTime?)h.Data)
            .FirstOrDefault();


    public string UltimaConsultaTexto
    {
        get
        {
            if (PrimeiraConsulta ||
                !UltimaConsulta.HasValue)
            {
                return "Primeira consulta";
            }


            return
                $"Última consulta: " +
                $"{UltimaConsulta.Value:dd/MM/yyyy}";
        }
    }
}


public class ConsultaHistoricoMedicoItem
{
    public int IdHistorico { get; set; }


    public string TipoConsulta { get; set; } =
        string.Empty;


    public DateTime Data { get; set; }


    public string Observacoes { get; set; } =
        string.Empty;


    public string DataTexto =>
        Data.ToString(
            "dd/MM/yyyy");
}