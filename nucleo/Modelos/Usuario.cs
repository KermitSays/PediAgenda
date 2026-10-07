namespace PediAgenda.Nucleo.Modelos;

/// <summary>
/// Classe base dos usuários do PediAgenda.
///
/// Médico e recepcionista possuem contas criadas pela clínica.
/// O responsável pode existir inicialmente apenas como pré-cadastro,
/// ainda sem e-mail, senha ou acesso liberado ao aplicativo.
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    public string Nome { get; set; } = "";

    public string Cpf { get; set; } = "";

    // Pode ser nulo enquanto o responsável ainda não concluiu
    // o cadastro de acesso ao aplicativo.
    public string? Email { get; set; }

    public Perfil Perfil { get; set; }

    // A senha nunca é armazenada em texto puro.
    // Responsáveis pré-cadastrados ainda não possuem senha,
    // portanto hash e salt podem ser nulos.
    public byte[]? SenhaHash { get; set; }

    public byte[]? SenhaSalt { get; set; }

    public int SenhaIteracoes { get; set; } = 100_000;

    // Por segurança, uma conta nova começa inativa.
    // Médico/recepção são ativados pela clínica.
    // Responsável será ativado após concluir o cadastro
    // e confirmar o e-mail.
    public bool Ativo { get; set; }

    // Controle da confirmação do endereço de e-mail.
    public bool EmailVerificado { get; set; }

    public byte[]? TokenVerificacaoHash { get; set; }

    public DateTime? TokenVerificacaoExpiraEm { get; set; }

    public DateTime? EmailVerificadoEm { get; set; }

    // RNF02 — bloquear acesso após 5 tentativas inválidas.
    public int TentativasInvalidas { get; set; }

    public DateTime? BloqueadoAte { get; set; }

    public bool EstaBloqueado(DateTime agora) =>
        BloqueadoAte is not null && BloqueadoAte > agora;

    /// <summary>
    /// Indica se o usuário já possui credenciais suficientes
    /// para tentar realizar login.
    /// </summary>
    public bool PossuiCredenciais =>
        !string.IsNullOrWhiteSpace(Email)
        && SenhaHash is { Length: > 0 }
        && SenhaSalt is { Length: > 0 };
}

public enum Perfil
{
    Responsavel = 1,
    Medico = 2,
    Recepcionista = 3
}