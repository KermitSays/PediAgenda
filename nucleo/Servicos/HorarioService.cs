using System.Globalization;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;

namespace PediAgenda.Nucleo.Servicos;

public record ResultadoHorarios(bool Sucesso, DateOnly Data, IReadOnlyList<HorarioLivre> Livres,
                                IReadOnlyDictionary<string, string>? Campos = null);

/// <summary>
/// Horários livres para agendar (RF05, RNF03).
///
/// "Em tempo real" aqui quer dizer: a lista é calculada na hora da consulta,
/// a partir das consultas que existem naquele momento — não é uma cópia
/// guardada que pode estar desatualizada. Hoje, só entram os horários que
/// ainda não começaram.
/// </summary>
public class HorarioService(IHorarioRepositorio repositorio, TimeProvider? relogio = null)
{
    private readonly IHorarioRepositorio _repo = repositorio;
    private readonly TimeProvider _relogio = relogio ?? TimeProvider.System;

    public ResultadoHorarios Livres(string? dataInformada, string? especialidade, int? idMedico)
    {
        var agora = _relogio.GetLocalNow().DateTime;
        var hoje = DateOnly.FromDateTime(agora);
        var campos = new Dictionary<string, string>();

        DateOnly data = default;
        if (string.IsNullOrWhiteSpace(dataInformada))
            campos["data"] = "Informe a data.";
        else if (!DateOnly.TryParseExact(dataInformada.Trim(), "yyyy-MM-dd",
                                         CultureInfo.InvariantCulture, DateTimeStyles.None, out data))
            campos["data"] = "Data inválida. Use o formato AAAA-MM-DD.";
        else if (data < hoje)
            campos["data"] = "Não dá para agendar em data que já passou.";

        if (campos.Count > 0)
            return new ResultadoHorarios(false, data, [], campos);

        var aPartirDe = data == hoje ? TimeOnly.FromDateTime(agora) : TimeOnly.MinValue;
        var filtroEspecialidade = string.IsNullOrWhiteSpace(especialidade) ? null : especialidade.Trim();

        return new ResultadoHorarios(true, data,
            _repo.ListarLivres(data, aPartirDe, filtroEspecialidade, idMedico));
    }
}
