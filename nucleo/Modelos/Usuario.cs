namespace PediAgenda.Nucleo.Modelos;

/// <summary>
/// Classe base da generalização descrita no item 14 da Declaração de Visão.
/// Espelha a tabela `usuario` de banco/03_modelo_completo.sql. O perfil não é
/// coluna da tabela: vem da especialização (medico, recepcionista, responsavel).
/// </summary>
public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Cpf { get; set; } = "";
    public string Email { get; set; } = "";
    public Perfil Perfil { get; set; }

    // A senha nunca é guardada em texto puro — só o hash e o salt.
    public byte[] SenhaHash { get; set; } = [];
    public byte[] SenhaSalt { get; set; } = [];
    public int SenhaIteracoes { get; set; } = 100_000;

    public bool Ativo { get; set; } = true;

    // RNF02 — bloquear acesso após 5 tentativas inválidas.
    public int TentativasInvalidas { get; set; }
    public DateTime? BloqueadoAte { get; set; }

    public bool EstaBloqueado(DateTime agora) => BloqueadoAte is not null && BloqueadoAte > agora;
}

public enum Perfil
{
    Responsavel = 1,
    Medico = 2,
    Recepcionista = 3
}
