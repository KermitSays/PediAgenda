using System.Globalization;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;

namespace PediAgenda.Nucleo.Servicos;

public enum MotivoRecusaPaciente
{
    Nenhum,
    NaoEResponsavel,
    DadosInvalidos,
    JaCadastrado
}

public record ResultadoPaciente(bool Sucesso, string Mensagem, MotivoRecusaPaciente Motivo,
                                Paciente? Paciente = null,
                                IReadOnlyDictionary<string, string>? Campos = null);

/// <summary>
/// Cadastro e consulta dos pacientes (as crianças) de um responsável.
///
/// O responsável nunca é informado pelo app: ele vem do token, pelo id do
/// usuário logado. Assim ninguém consegue listar ou cadastrar filhos em nome de
/// outra família trocando um número na chamada.
/// </summary>
public class PacienteService(IPacienteRepositorio repositorio, TimeProvider? relogio = null)
{
    public const int TamanhoMaximoNome = 120;

    // A clínica é pediátrica: atende de recém-nascidos até 17 anos completos.
    public const int IdadeMaxima = 17;

    private readonly IPacienteRepositorio _repo = repositorio;
    private readonly TimeProvider _relogio = relogio ?? TimeProvider.System;

    /// <summary>
    /// Os pacientes do responsável logado, ou null se o usuário não for responsável.
    /// </summary>
    public IReadOnlyList<Paciente>? Listar(int idUsuario)
    {
        var idResponsavel = _repo.IdResponsavelDoUsuario(idUsuario);
        return idResponsavel is null ? null : _repo.ListarPorResponsavel(idResponsavel.Value);
    }

    public ResultadoPaciente Cadastrar(int idUsuario, string? nomeInformado, string? dataInformada)
    {
        var idResponsavel = _repo.IdResponsavelDoUsuario(idUsuario);
        if (idResponsavel is null)
            return NaoEResponsavel();

        var nome = (nomeInformado ?? "").Trim();
        var hoje = DateOnly.FromDateTime(_relogio.GetLocalNow().DateTime);
        var campos = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(nome))
            campos["nome"] = "O nome é obrigatório.";
        else if (nome.Length > TamanhoMaximoNome)
            campos["nome"] = $"O nome deve ter no máximo {TamanhoMaximoNome} caracteres.";

        DateOnly nascimento = default;
        if (string.IsNullOrWhiteSpace(dataInformada))
            campos["dataNascimento"] = "A data de nascimento é obrigatória.";
        else if (!DateOnly.TryParseExact(dataInformada.Trim(), "yyyy-MM-dd",
                                         CultureInfo.InvariantCulture, DateTimeStyles.None, out nascimento))
            campos["dataNascimento"] = "Data inválida. Use o formato AAAA-MM-DD.";
        else if (nascimento > hoje)
            campos["dataNascimento"] = "A data de nascimento não pode estar no futuro.";
        else if (new Paciente { DataNascimento = nascimento }.IdadeEm(hoje) > IdadeMaxima)
            campos["dataNascimento"] = $"A clínica é pediátrica: atende pacientes de até {IdadeMaxima} anos.";

        if (campos.Count > 0)
            return new ResultadoPaciente(false, "Verifique os campos destacados.",
                                         MotivoRecusaPaciente.DadosInvalidos, Campos: campos);

        // Evita o mesmo filho cadastrado duas vezes, por exemplo com um toque
        // duplo no botão de salvar.
        if (_repo.Existe(idResponsavel.Value, nome, nascimento))
            return new ResultadoPaciente(false, "Esse paciente já está cadastrado.",
                                         MotivoRecusaPaciente.JaCadastrado);

        var id = _repo.Inserir(idResponsavel.Value, nome, nascimento);
        var paciente = new Paciente
        {
            Id = id,
            Nome = nome,
            DataNascimento = nascimento,
            IdResponsavel = idResponsavel.Value
        };
        return new ResultadoPaciente(true, $"{nome} foi cadastrado(a).", MotivoRecusaPaciente.Nenhum, paciente);
    }

    private static ResultadoPaciente NaoEResponsavel() =>
        new(false, "Só o responsável pode cadastrar e ver pacientes.", MotivoRecusaPaciente.NaoEResponsavel);
}
