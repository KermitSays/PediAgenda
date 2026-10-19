using System.Text;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;

namespace PediAgenda.Nucleo.Servicos;

public class ProntuarioService(IProntuarioRepositorio registros, IAgendaRepositorio agenda,
    TimeProvider relogio)
{
    public ResultadoClinica<IReadOnlyList<PacienteDoMedico>> Pacientes(Ator ator)
    {
        var medico = Medico(ator);
        return medico is null ? SemMedico<IReadOnlyList<PacienteDoMedico>>()
            : ResultadoClinica<IReadOnlyList<PacienteDoMedico>>.Ok(registros.Pacientes(medico.Value));
    }

    public ResultadoClinica<IReadOnlyList<Prontuario>> Historico(Ator ator, int idPaciente)
    {
        var medico = Medico(ator);
        if (medico is null) return SemMedico<IReadOnlyList<Prontuario>>();
        if (!registros.TemPaciente(medico.Value, idPaciente))
            return ResultadoClinica<IReadOnlyList<Prontuario>>.Erro(TipoFalhaClinica.NaoEncontrado,
                "PACIENTE_NAO_ENCONTRADO", "Paciente não encontrado na sua agenda.");
        return ResultadoClinica<IReadOnlyList<Prontuario>>.Ok(registros.Historico(medico.Value, idPaciente));
    }

    public ResultadoClinica<Prontuario> Buscar(Ator ator, int idConsulta)
    {
        var medico = Medico(ator);
        if (medico is null) return SemMedico<Prontuario>();
        var registro = registros.Buscar(medico.Value, idConsulta);
        return registro is null
            ? ResultadoClinica<Prontuario>.Erro(TipoFalhaClinica.NaoEncontrado,
                "PRONTUARIO_NAO_ENCONTRADO", "Prontuário não encontrado na sua agenda.")
            : ResultadoClinica<Prontuario>.Ok(registro);
    }

    public ResultadoClinica<Prontuario> Registrar(Ator ator, int idConsulta, string? texto)
    {
        var medico = Medico(ator);
        if (medico is null) return SemMedico<Prontuario>();
        texto = (texto ?? "").Trim();
        // TEXT armazena no máximo 65.535 bytes, inclusive em utf8mb4.
        if (texto.Length == 0 || Encoding.UTF8.GetByteCount(texto) > 65535)
            return ResultadoClinica<Prontuario>.Invalido(new Dictionary<string, string>
            {
                ["observacoes"] = "Preencha as observações com até 65.535 bytes de texto UTF-8."
            });
        var resultado = registros.Registrar(medico.Value, idConsulta, texto,
            relogio.GetLocalNow().DateTime, relogio.GetUtcNow().UtcDateTime);
        return resultado switch
        {
            ResultadoRegistroProntuario.Registrado => Buscar(ator, idConsulta),
            ResultadoRegistroProntuario.NaoEncontrada => ResultadoClinica<Prontuario>.Erro(
                TipoFalhaClinica.NaoEncontrado, "CONSULTA_NAO_ENCONTRADA", "Consulta não encontrada na sua agenda."),
            ResultadoRegistroProntuario.JaRegistrado => ResultadoClinica<Prontuario>.Erro(
                TipoFalhaClinica.Conflito, "PRONTUARIO_JA_REGISTRADO", "Essa consulta já possui prontuário."),
            ResultadoRegistroProntuario.Cancelada => ResultadoClinica<Prontuario>.Erro(
                TipoFalhaClinica.Conflito, "CONSULTA_CANCELADA", "Não é possível atender uma consulta cancelada."),
            _ => ResultadoClinica<Prontuario>.Erro(TipoFalhaClinica.Conflito,
                "CONSULTA_NAO_COMECOU", "A consulta ainda não começou.")
        };
    }

    private int? Medico(Ator ator) => ator.EhMedico ? agenda.IdMedicoDoUsuario(ator.IdUsuario) : null;

    private static ResultadoClinica<T> SemMedico<T>() => ResultadoClinica<T>.Erro(
        TipoFalhaClinica.SemPermissao, "SEM_PERMISSAO", "Seu usuário não está ligado a um médico.");
}
