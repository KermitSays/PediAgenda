using System.Globalization;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;

namespace PediAgenda.Nucleo.Servicos;

public enum TipoFalhaClinica
{
    Nenhuma,
    DadosInvalidos,
    SemPermissao,
    NaoEncontrado,
    Conflito
}

public record ResultadoClinica<T>(bool Sucesso, TipoFalhaClinica Falha, string Codigo, string Mensagem,
                                  T? Valor = default, IReadOnlyDictionary<string, string>? Campos = null)
{
    public static ResultadoClinica<T> Ok(T valor, string mensagem = "") =>
        new(true, TipoFalhaClinica.Nenhuma, "", mensagem, valor);

    public static ResultadoClinica<T> Erro(TipoFalhaClinica falha, string codigo, string mensagem) =>
        new(false, falha, codigo, mensagem);

    public static ResultadoClinica<T> Invalido(IReadOnlyDictionary<string, string> campos) =>
        new(false, TipoFalhaClinica.DadosInvalidos, "DADOS_INVALIDOS", "Verifique os campos destacados.",
            Campos: campos);
}

public record HorariosAbertos(int Criados, int Ignorados);

/// <summary>
/// O lado da clínica (RF08, RF09): abrir horários, ver a agenda do dia,
/// bloquear e desbloquear, confirmar e dar a consulta como realizada.
///
/// Regra de acesso: o médico só mexe na própria agenda — o id do médico vem
/// do token, e o que vier na chamada é ignorado. A recepção mexe em todas, e
/// por isso precisa dizer de qual médico está falando.
/// </summary>
public class AgendaService(IAgendaRepositorio repositorio, TimeProvider? relogio = null)
{
    public static readonly IReadOnlySet<int> DuracoesPermitidas = new HashSet<int> { 15, 20, 30, 40, 45, 60 };
    public const int DuracaoPadrao = 30;

    private readonly IAgendaRepositorio _repo = repositorio;
    private readonly TimeProvider _relogio = relogio ?? TimeProvider.System;

    private DateTime Agora => _relogio.GetLocalNow().DateTime;

    public ResultadoClinica<HorariosAbertos> AbrirHorarios(Ator ator, int? idMedicoInformado, string? dataInformada,
                                                           string? inicioInformado, string? fimInformado,
                                                           int? duracaoInformada)
    {
        var campos = new Dictionary<string, string>();
        var (idMedico, erroMedico) = ResolverMedico(ator, idMedicoInformado, campos);
        if (erroMedico is not null)
            return ResultadoClinica<HorariosAbertos>.Erro(erroMedico.Value.Falha, erroMedico.Value.Codigo,
                                                          erroMedico.Value.Mensagem);

        var hoje = DateOnly.FromDateTime(Agora);
        var data = LerData(dataInformada, "data", campos);
        if (data is not null && data < hoje)
            campos["data"] = "Não dá para abrir horários em data que já passou.";

        var inicio = LerHora(inicioInformado, "inicio", campos);
        var fim = LerHora(fimInformado, "fim", campos);
        if (inicio is not null && fim is not null && fim <= inicio)
            campos["fim"] = "O fim precisa ser depois do início.";

        var duracao = duracaoInformada ?? DuracaoPadrao;
        if (!DuracoesPermitidas.Contains(duracao))
            campos["duracao"] = "Duração em minutos: 15, 20, 30, 40, 45 ou 60.";

        if (campos.Count > 0)
            return ResultadoClinica<HorariosAbertos>.Invalido(campos);

        // Blocos de "duracao" minutos que cabem inteiros entre início e fim. Se
        // for hoje, só os que ainda não começaram.
        var blocos = new List<(TimeOnly, TimeOnly)>();
        var agora = TimeOnly.FromDateTime(Agora);
        for (var minuto = inicio!.Value.Hour * 60 + inicio.Value.Minute;
             minuto + duracao <= fim!.Value.Hour * 60 + fim.Value.Minute;
             minuto += duracao)
        {
            var bloco = new TimeOnly(minuto / 60, minuto % 60);
            if (data == hoje && bloco <= agora)
                continue;
            blocos.Add((bloco, bloco.AddMinutes(duracao)));
        }

        if (blocos.Count == 0)
            return ResultadoClinica<HorariosAbertos>.Invalido(new Dictionary<string, string>
            {
                ["fim"] = "Nenhum bloco cabe nesse intervalo."
            });

        var idAgenda = _repo.IdAgendaDoMedico(idMedico!.Value);
        if (idAgenda is null)
            return ResultadoClinica<HorariosAbertos>.Erro(TipoFalhaClinica.NaoEncontrado, "MEDICO_NAO_ENCONTRADO",
                                                          "Médico não encontrado.");

        var criados = _repo.AbrirHorarios(idAgenda.Value, data!.Value, blocos);
        return ResultadoClinica<HorariosAbertos>.Ok(new HorariosAbertos(criados, blocos.Count - criados));
    }

    public ResultadoClinica<IReadOnlyList<ItemAgenda>> AgendaDoDia(Ator ator, string? dataInformada, int? idMedicoInformado)
    {
        var campos = new Dictionary<string, string>();
        var data = LerData(dataInformada, "data", campos);
        if (campos.Count > 0)
            return ResultadoClinica<IReadOnlyList<ItemAgenda>>.Invalido(campos);

        // Médico: sempre a própria agenda. Recepção: todas, ou a do médico pedido.
        int? filtro = idMedicoInformado;
        if (ator.EhMedico)
        {
            filtro = _repo.IdMedicoDoUsuario(ator.IdUsuario);
            if (filtro is null)
                return ResultadoClinica<IReadOnlyList<ItemAgenda>>.Erro(TipoFalhaClinica.SemPermissao,
                    "SEM_PERMISSAO", "Seu usuário não está ligado a um médico.");
        }

        return ResultadoClinica<IReadOnlyList<ItemAgenda>>.Ok(_repo.AgendaDoDia(data!.Value, filtro));
    }

    public ResultadoClinica<bool> AlterarBloqueio(Ator ator, int idHorario, bool bloquear)
    {
        int? somenteDoMedico = null;
        if (ator.EhMedico)
        {
            somenteDoMedico = _repo.IdMedicoDoUsuario(ator.IdUsuario);
            if (somenteDoMedico is null)
                return ResultadoClinica<bool>.Erro(TipoFalhaClinica.SemPermissao, "SEM_PERMISSAO",
                                                   "Seu usuário não está ligado a um médico.");
        }

        return _repo.AlterarDisponibilidade(idHorario, somenteDoMedico, disponivel: !bloquear) switch
        {
            ResultadoBloqueio.Alterado => ResultadoClinica<bool>.Ok(bloquear,
                bloquear ? "Horário bloqueado." : "Horário liberado."),
            ResultadoBloqueio.NaoEncontrado => ResultadoClinica<bool>.Erro(TipoFalhaClinica.NaoEncontrado,
                "HORARIO_NAO_ENCONTRADO", "Horário não encontrado."),
            ResultadoBloqueio.DeOutroMedico => ResultadoClinica<bool>.Erro(TipoFalhaClinica.SemPermissao,
                "SEM_PERMISSAO", "Esse horário é da agenda de outro médico."),
            ResultadoBloqueio.TemConsulta => ResultadoClinica<bool>.Erro(TipoFalhaClinica.Conflito,
                "HORARIO_OCUPADO", "Esse horário tem consulta marcada. Cancele a consulta antes de bloquear."),
            var outro => throw new InvalidOperationException($"Resultado inesperado: {outro}")
        };
    }

    /// <summary>
    /// A recepção (ou o médico) confirma com a família uma consulta que ainda vai acontecer.
    /// </summary>
    public ResultadoClinica<string> Confirmar(Ator ator, int idConsulta) =>
        Transicao(ator, idConsulta,
                  permitidos: new HashSet<string> { StatusConsulta.Agendada },
                  novo: StatusConsulta.Confirmada, exigeJaTerComecado: false,
                  mensagemStatus: "Só dá para confirmar consulta agendada.");

    /// <summary>
    /// O médico registra que atendeu. É o que alimenta os relatórios de atendimento.
    /// </summary>
    public ResultadoClinica<string> Realizar(Ator ator, int idConsulta) =>
        Transicao(ator, idConsulta,
                  permitidos: new HashSet<string> { StatusConsulta.Agendada, StatusConsulta.Confirmada },
                  novo: StatusConsulta.Realizada, exigeJaTerComecado: true,
                  mensagemStatus: "Só dá para registrar como realizada uma consulta agendada ou confirmada.");

    private ResultadoClinica<string> Transicao(Ator ator, int idConsulta, IReadOnlySet<string> permitidos,
                                              string novo, bool exigeJaTerComecado, string mensagemStatus)
    {
        int? somenteDoMedico = null;
        if (ator.EhMedico)
        {
            somenteDoMedico = _repo.IdMedicoDoUsuario(ator.IdUsuario);
            if (somenteDoMedico is null)
                return ResultadoClinica<string>.Erro(TipoFalhaClinica.SemPermissao, "SEM_PERMISSAO",
                                                     "Seu usuário não está ligado a um médico.");
        }

        return _repo.MudarStatusConsulta(idConsulta, somenteDoMedico, permitidos, novo, Agora, exigeJaTerComecado) switch
        {
            ResultadoTransicao.Alterada => ResultadoClinica<string>.Ok(novo),
            ResultadoTransicao.NaoEncontrada => ResultadoClinica<string>.Erro(TipoFalhaClinica.NaoEncontrado,
                "CONSULTA_NAO_ENCONTRADA", "Consulta não encontrada."),
            ResultadoTransicao.DeOutroMedico => ResultadoClinica<string>.Erro(TipoFalhaClinica.SemPermissao,
                "SEM_PERMISSAO", "Essa consulta é de outro médico."),
            ResultadoTransicao.StatusNaoPermite => ResultadoClinica<string>.Erro(TipoFalhaClinica.Conflito,
                "STATUS_NAO_PERMITE", mensagemStatus),
            ResultadoTransicao.AindaNaoComecou => ResultadoClinica<string>.Erro(TipoFalhaClinica.Conflito,
                "CONSULTA_NAO_COMECOU", "A consulta ainda não começou."),
            ResultadoTransicao.JaComecou => ResultadoClinica<string>.Erro(TipoFalhaClinica.Conflito,
                "CONSULTA_JA_COMECOU", "A consulta já começou."),
            var outro => throw new InvalidOperationException($"Resultado inesperado: {outro}")
        };
    }

    private (int? IdMedico, (TipoFalhaClinica Falha, string Codigo, string Mensagem)? Erro) ResolverMedico(
        Ator ator, int? idMedicoInformado, Dictionary<string, string> campos)
    {
        if (ator.EhMedico)
        {
            var proprio = _repo.IdMedicoDoUsuario(ator.IdUsuario);
            return proprio is null
                ? (null, (TipoFalhaClinica.SemPermissao, "SEM_PERMISSAO", "Seu usuário não está ligado a um médico."))
                : (proprio, null);
        }

        if (idMedicoInformado is null or <= 0)
            campos["idMedico"] = "Informe o médico.";
        return (idMedicoInformado, null);
    }

    private static DateOnly? LerData(string? valor, string campo, Dictionary<string, string> campos)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            campos[campo] = "Informe a data.";
            return null;
        }
        if (DateOnly.TryParseExact(valor.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var data))
            return data;

        campos[campo] = "Data inválida. Use o formato AAAA-MM-DD.";
        return null;
    }

    private static TimeOnly? LerHora(string? valor, string campo, Dictionary<string, string> campos)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            campos[campo] = "Informe a hora.";
            return null;
        }
        if (TimeOnly.TryParseExact(valor.Trim(), "HH:mm", CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var hora))
            return hora;

        campos[campo] = "Hora inválida. Use o formato HH:MM.";
        return null;
    }
}
