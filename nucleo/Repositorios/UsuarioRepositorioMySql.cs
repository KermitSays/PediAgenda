using MySqlConnector;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Seguranca;

namespace PediAgenda.Nucleo.Repositorios;

/// <summary>
/// Implementação real sobre as tabelas de banco/03_modelo_completo.sql.
///
/// Implementa a MESMA interface do repositório em memória — por isso o
/// AutenticacaoService não muda uma linha ao trocar um pelo outro.
/// </summary>
public class UsuarioRepositorioMySql(string stringDeConexao) : IUsuarioRepositorio
{
    private readonly string _conexao = stringDeConexao;

    private MySqlConnection Abrir()
    {
        var con = new MySqlConnection(_conexao);
        con.Open();
        return con;
    }

    public Usuario? BuscarPorEmail(string email)
    {
        // Não existe mais tabela de perfis: o perfil é deduzido de qual
        // especialização (medico, recepcionista, responsavel) aponta para o usuário.
        const string sql = """
            SELECT  u.id_usuario, u.nome, u.cpf, u.email,
                    u.senha_hash, u.senha_salt, u.senha_iteracoes,
                    u.ativo, u.tentativas_invalidas, u.bloqueado_ate,
                    CASE
                        WHEN m.id_medico        IS NOT NULL THEN 'MEDICO'
                        WHEN rc.id_recepcionista IS NOT NULL THEN 'RECEPCIONISTA'
                        WHEN rs.id_responsavel  IS NOT NULL THEN 'RESPONSAVEL'
                    END AS perfil
            FROM      usuario u
            LEFT JOIN medico        m  ON m.id_usuario  = u.id_usuario
            LEFT JOIN recepcionista rc ON rc.id_usuario = u.id_usuario
            LEFT JOIN responsavel   rs ON rs.id_usuario = u.id_usuario
            WHERE     u.email = @email
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@email", email);   // parametrizado: sem SQL injection

        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return null;

        var semPerfil = r.IsDBNull(r.GetOrdinal("perfil"));

        return new Usuario
        {
            Id = r.GetInt32("id_usuario"),
            Nome = r.GetString("nome"),
            Cpf = r.GetString("cpf"),
            Email = r.GetString("email"),
            Perfil = semPerfil ? default : ParaPerfil(r.GetString("perfil")),
            SenhaHash = (byte[])r["senha_hash"],
            SenhaSalt = (byte[])r["senha_salt"],
            SenhaIteracoes = r.GetInt32("senha_iteracoes"),
            // Usuário sem nenhuma especialização não tem o que fazer no sistema:
            // é tratado como conta inativa, e o login devolve a mensagem correspondente.
            Ativo = r.GetBoolean("ativo") && !semPerfil,
            TentativasInvalidas = r.GetInt32("tentativas_invalidas"),
            BloqueadoAte = r.IsDBNull(r.GetOrdinal("bloqueado_ate")) ? null : r.GetDateTime("bloqueado_ate")
        };
    }

    public void Atualizar(Usuario usuario)
    {
        const string sql = """
            UPDATE  usuario
            SET     tentativas_invalidas = @tentativas,
                    bloqueado_ate        = @bloqueado
            WHERE   id_usuario = @id
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@tentativas", usuario.TentativasInvalidas);
        cmd.Parameters.AddWithValue("@bloqueado", (object?)usuario.BloqueadoAte ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", usuario.Id);
        cmd.ExecuteNonQuery();
    }

    public void RegistrarTentativa(string emailInformado, int? usuarioId, bool sucesso, string? motivo)
    {
        const string sql = """
            INSERT INTO tentativa_login (id_usuario, email_informado, sucesso, motivo)
            VALUES (@usuario, @email, @sucesso, @motivo)
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@usuario", (object?)usuarioId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@email", emailInformado);
        cmd.Parameters.AddWithValue("@sucesso", sucesso);
        cmd.Parameters.AddWithValue("@motivo", (object?)motivo ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Cria um usuário já com a senha em hash e a especialização do perfil.
    /// É o que o cadastro de responsável usa, e também o comando de massa de teste.
    ///
    /// São dois INSERTs (usuario + especialização) dentro de uma transação:
    /// se o segundo falhar, o primeiro é desfeito e não sobra usuário sem perfil.
    /// </summary>
    public int Inserir(string nome, string cpf, string email, string senha, Perfil perfil,
                       string? telefone = null, string? crm = null)
    {
        if (!PoliticaSenha.Valida(senha, out var erro))
            throw new ArgumentException(erro, nameof(senha));
        if (perfil == Perfil.Responsavel && string.IsNullOrWhiteSpace(telefone))
            throw new ArgumentException("Responsável exige telefone.", nameof(telefone));
        if (perfil == Perfil.Medico && string.IsNullOrWhiteSpace(crm))
            throw new ArgumentException("Médico exige CRM.", nameof(crm));

        var (hash, salt, iteracoes) = HashSenha.Gerar(senha);

        try
        {
            return InserirComTransacao(nome, cpf, email, hash, salt, iteracoes, perfil, telefone, crm);
        }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            // Entre conferir e gravar, outro cadastro pode ter usado o mesmo
            // e-mail. Quem decide de verdade é a restrição UNIQUE do banco.
            var campo = e.Message.Contains("cpf", StringComparison.OrdinalIgnoreCase) ? "CPF" : "e-mail";
            throw new CadastroDuplicadoException(campo);
        }
    }

    private int InserirComTransacao(string nome, string cpf, string email,
                                    byte[] hash, byte[] salt, int iteracoes,
                                    Perfil perfil, string? telefone, string? crm)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();

        using var cmdUsuario = new MySqlCommand("""
            INSERT INTO usuario (nome, cpf, email, senha_hash, senha_salt, senha_iteracoes)
            VALUES (@nome, @cpf, @email, @hash, @salt, @iteracoes)
            """, con, tx);
        cmdUsuario.Parameters.AddWithValue("@nome", nome);
        cmdUsuario.Parameters.AddWithValue("@cpf", cpf);
        cmdUsuario.Parameters.AddWithValue("@email", email);
        cmdUsuario.Parameters.AddWithValue("@hash", hash);
        cmdUsuario.Parameters.AddWithValue("@salt", salt);
        cmdUsuario.Parameters.AddWithValue("@iteracoes", iteracoes);
        cmdUsuario.ExecuteNonQuery();
        var idUsuario = (int)cmdUsuario.LastInsertedId;

        var sqlPerfil = perfil switch
        {
            Perfil.Responsavel   => "INSERT INTO responsavel (telefone, id_usuario) VALUES (@telefone, @id)",
            Perfil.Medico        => "INSERT INTO medico (crm, id_usuario) VALUES (@crm, @id)",
            Perfil.Recepcionista => "INSERT INTO recepcionista (id_usuario) VALUES (@id)",
            _ => throw new ArgumentOutOfRangeException(nameof(perfil))
        };
        using var cmdPerfil = new MySqlCommand(sqlPerfil, con, tx);
        cmdPerfil.Parameters.AddWithValue("@id", idUsuario);
        cmdPerfil.Parameters.AddWithValue("@telefone", (object?)telefone ?? DBNull.Value);
        cmdPerfil.Parameters.AddWithValue("@crm", (object?)crm ?? DBNull.Value);
        cmdPerfil.ExecuteNonQuery();

        tx.Commit();
        return idUsuario;
    }

    public bool ExisteEmail(string email) =>
        Contar("SELECT COUNT(*) FROM usuario WHERE email = @valor", email);

    public bool ExisteCpf(string cpf) =>
        Contar("SELECT COUNT(*) FROM usuario WHERE cpf = @valor", cpf);

    private bool Contar(string sql, string valor)
    {
        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);
        cmd.Parameters.AddWithValue("@valor", valor);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private static Perfil ParaPerfil(string nome) => nome switch
    {
        "RESPONSAVEL" => Perfil.Responsavel,
        "MEDICO" => Perfil.Medico,
        "RECEPCIONISTA" => Perfil.Recepcionista,
        _ => throw new InvalidOperationException($"Perfil desconhecido: {nome}")
    };
}
