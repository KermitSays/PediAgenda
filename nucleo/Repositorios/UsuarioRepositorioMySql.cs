using MySqlConnector;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Seguranca;

namespace PediAgenda.Nucleo.Repositorios;

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
        const string sql = """
            SELECT
                u.id_usuario,
                u.nome,
                u.cpf,
                u.email,
                u.senha_hash,
                u.senha_salt,
                u.senha_iteracoes,
                u.ativo,
                u.email_verificado,
                u.token_verificacao_hash,
                u.token_verificacao_expira_em,
                u.email_verificado_em,
                u.tentativas_invalidas,
                u.bloqueado_ate,

                CASE
                    WHEN m.id_medico IS NOT NULL
                        THEN 'MEDICO'

                    WHEN rc.id_recepcionista IS NOT NULL
                        THEN 'RECEPCIONISTA'

                    WHEN rs.id_responsavel IS NOT NULL
                        THEN 'RESPONSAVEL'
                END AS perfil

            FROM usuario u

            LEFT JOIN medico m
                ON m.id_usuario = u.id_usuario

            LEFT JOIN recepcionista rc
                ON rc.id_usuario = u.id_usuario

            LEFT JOIN responsavel rs
                ON rs.id_usuario = u.id_usuario

            WHERE u.email = @email

            LIMIT 1
            """;

        using var con = Abrir();
        using var cmd = new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@email",
            email);

        using var r = cmd.ExecuteReader();

        if (!r.Read())
            return null;

        var semPerfil =
            r.IsDBNull(
                r.GetOrdinal("perfil"));

        return new Usuario
        {
            Id =
                r.GetInt32("id_usuario"),

            Nome =
                r.GetString("nome"),

            Cpf =
                r.GetString("cpf"),

            Email =
                r.IsDBNull(r.GetOrdinal("email"))
                    ? null
                    : r.GetString("email"),

            Perfil =
                semPerfil
                    ? default
                    : ParaPerfil(
                        r.GetString("perfil")),

            SenhaHash =
                r.IsDBNull(r.GetOrdinal("senha_hash"))
                    ? null
                    : (byte[])r["senha_hash"],

            SenhaSalt =
                r.IsDBNull(r.GetOrdinal("senha_salt"))
                    ? null
                    : (byte[])r["senha_salt"],

            SenhaIteracoes =
                r.GetInt32("senha_iteracoes"),

            Ativo =
                r.GetBoolean("ativo")
                && !semPerfil,

            EmailVerificado =
                r.GetBoolean(
                    "email_verificado"),

            TokenVerificacaoHash =
                r.IsDBNull(
                    r.GetOrdinal(
                        "token_verificacao_hash"))
                            ? null
                            : (byte[])r[
                                "token_verificacao_hash"],

            TokenVerificacaoExpiraEm =
                r.IsDBNull(
                    r.GetOrdinal(
                        "token_verificacao_expira_em"))
                            ? null
                            : r.GetDateTime(
                                "token_verificacao_expira_em"),

            EmailVerificadoEm =
                r.IsDBNull(
                    r.GetOrdinal(
                        "email_verificado_em"))
                            ? null
                            : r.GetDateTime(
                                "email_verificado_em"),

            TentativasInvalidas =
                r.GetInt32(
                    "tentativas_invalidas"),

            BloqueadoAte =
                r.IsDBNull(
                    r.GetOrdinal("bloqueado_ate"))
                        ? null
                        : r.GetDateTime(
                            "bloqueado_ate")
        };
    }

    public Usuario? BuscarResponsavelPorCpf(
        string cpf)
    {
        const string sql = """
            SELECT
                u.id_usuario,
                u.nome,
                u.cpf,
                u.email,
                u.senha_hash,
                u.senha_salt,
                u.senha_iteracoes,
                u.ativo,
                u.email_verificado,
                u.token_verificacao_hash,
                u.token_verificacao_expira_em,
                u.email_verificado_em,
                u.tentativas_invalidas,
                u.bloqueado_ate

            FROM usuario u

            INNER JOIN responsavel r
                ON r.id_usuario = u.id_usuario

            WHERE u.cpf = @cpf

            LIMIT 1
            """;

        using var con = Abrir();
        using var cmd =
            new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@cpf",
            cpf);

        using var r =
            cmd.ExecuteReader();

        if (!r.Read())
            return null;

        return new Usuario
        {
            Id =
                r.GetInt32("id_usuario"),

            Nome =
                r.GetString("nome"),

            Cpf =
                r.GetString("cpf"),

            Email =
                r.IsDBNull(r.GetOrdinal("email"))
                    ? null
                    : r.GetString("email"),

            Perfil =
                Perfil.Responsavel,

            SenhaHash =
                r.IsDBNull(
                    r.GetOrdinal("senha_hash"))
                        ? null
                        : (byte[])r["senha_hash"],

            SenhaSalt =
                r.IsDBNull(
                    r.GetOrdinal("senha_salt"))
                        ? null
                        : (byte[])r["senha_salt"],

            SenhaIteracoes =
                r.GetInt32(
                    "senha_iteracoes"),

            Ativo =
                r.GetBoolean("ativo"),

            EmailVerificado =
                r.GetBoolean(
                    "email_verificado"),

            TokenVerificacaoHash =
                r.IsDBNull(
                    r.GetOrdinal(
                        "token_verificacao_hash"))
                            ? null
                            : (byte[])r[
                                "token_verificacao_hash"],

            TokenVerificacaoExpiraEm =
                r.IsDBNull(
                    r.GetOrdinal(
                        "token_verificacao_expira_em"))
                            ? null
                            : r.GetDateTime(
                                "token_verificacao_expira_em"),

            EmailVerificadoEm =
                r.IsDBNull(
                    r.GetOrdinal(
                        "email_verificado_em"))
                            ? null
                            : r.GetDateTime(
                                "email_verificado_em"),

            TentativasInvalidas =
                r.GetInt32(
                    "tentativas_invalidas"),

            BloqueadoAte =
                r.IsDBNull(
                    r.GetOrdinal("bloqueado_ate"))
                        ? null
                        : r.GetDateTime(
                            "bloqueado_ate")
        };
    }

    public bool CodigoAtivacaoResponsavelValido(
        int usuarioId,
        string codigo,
        DateTime agora)
    {
        const string sql = """
            SELECT
                codigo_ativacao_hash,
                codigo_ativacao_expira_em,
                codigo_ativacao_usado_em

            FROM responsavel

            WHERE id_usuario = @id

            LIMIT 1
            """;

        using var con = Abrir();
        using var cmd =
            new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@id",
            usuarioId);

        using var r =
            cmd.ExecuteReader();

        if (!r.Read())
            return false;

        if (r.IsDBNull(
            r.GetOrdinal(
                "codigo_ativacao_hash")))
        {
            return false;
        }

        if (r.IsDBNull(
            r.GetOrdinal(
                "codigo_ativacao_expira_em")))
        {
            return false;
        }

        if (!r.IsDBNull(
            r.GetOrdinal(
                "codigo_ativacao_usado_em")))
        {
            return false;
        }

        var expiraEm =
            r.GetDateTime(
                "codigo_ativacao_expira_em");

        if (expiraEm <= agora)
            return false;

        var hashEsperado =
            (byte[])r[
                "codigo_ativacao_hash"];

        return CodigoAtivacao.Conferir(
            codigo,
            hashEsperado);
    }

    public void MarcarCodigoAtivacaoComoUsado(
        int usuarioId,
        DateTime usadoEm)
    {
        const string sql = """
            UPDATE responsavel

            SET codigo_ativacao_usado_em = @usadoEm

            WHERE id_usuario = @id
            """;

        using var con = Abrir();
        using var cmd =
            new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@usadoEm",
            usadoEm);

        cmd.Parameters.AddWithValue(
            "@id",
            usuarioId);

        cmd.ExecuteNonQuery();
    }

    public void CompletarCadastroResponsavel(
        int usuarioId,
        string email,
        string senha,
        byte[] tokenVerificacaoHash,
        DateTime tokenExpiraEm)
    {
        if (!PoliticaSenha.Valida(
            senha,
            out var erro))
        {
            throw new ArgumentException(
                erro,
                nameof(senha));
        }

        var (hash, salt, iteracoes) =
            HashSenha.Gerar(senha);

        const string sql = """
            UPDATE usuario u

            INNER JOIN responsavel r
                ON r.id_usuario = u.id_usuario

            SET
                u.email = @email,
                u.senha_hash = @hash,
                u.senha_salt = @salt,
                u.senha_iteracoes = @iteracoes,

                u.ativo = 0,
                u.email_verificado = 0,

                u.token_verificacao_hash = @token,
                u.token_verificacao_expira_em = @expira,
                u.email_verificado_em = NULL,

                u.tentativas_invalidas = 0,
                u.bloqueado_ate = NULL

            WHERE u.id_usuario = @id
            """;

        try
        {
            using var con = Abrir();
            using var cmd =
                new MySqlCommand(sql, con);

            cmd.Parameters.AddWithValue(
                "@email",
                email);

            cmd.Parameters.AddWithValue(
                "@hash",
                hash);

            cmd.Parameters.AddWithValue(
                "@salt",
                salt);

            cmd.Parameters.AddWithValue(
                "@iteracoes",
                iteracoes);

            cmd.Parameters.AddWithValue(
                "@token",
                tokenVerificacaoHash);

            cmd.Parameters.AddWithValue(
                "@expira",
                tokenExpiraEm);

            cmd.Parameters.AddWithValue(
                "@id",
                usuarioId);

            var alterados =
                cmd.ExecuteNonQuery();

            if (alterados == 0)
            {
                throw new InvalidOperationException(
                    "O responsável informado não foi encontrado.");
            }
        }
        catch (MySqlException e)
            when (
                e.ErrorCode
                == MySqlErrorCode.DuplicateKeyEntry)
        {
            throw new CadastroDuplicadoException(
                "e-mail");
        }
    }

    public bool ConfirmarEmailResponsavel(
    byte[] tokenHash,
    DateTime confirmadoEm)
    {
        const string sql = """
        UPDATE usuario u

        INNER JOIN responsavel r
            ON r.id_usuario = u.id_usuario

        SET
            u.email_verificado = 1,
            u.email_verificado_em = @confirmadoEm,
            u.ativo = 1,

            u.token_verificacao_hash = NULL,
            u.token_verificacao_expira_em = NULL,

            u.tentativas_invalidas = 0,
            u.bloqueado_ate = NULL,

            r.codigo_ativacao_usado_em = @confirmadoEm

        WHERE u.token_verificacao_hash = @token

          AND u.token_verificacao_expira_em IS NOT NULL

          AND u.token_verificacao_expira_em >= @confirmadoEm

          AND u.email_verificado = 0

          AND u.email IS NOT NULL

          AND u.senha_hash IS NOT NULL

          AND u.senha_salt IS NOT NULL

          AND r.codigo_ativacao_usado_em IS NULL
        """;

        using var con = Abrir();

        using var cmd =
            new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@token",
            tokenHash);

        cmd.Parameters.AddWithValue(
            "@confirmadoEm",
            confirmadoEm);

        var alterados =
            cmd.ExecuteNonQuery();

        return alterados > 0;
    }

    public void Atualizar(
        Usuario usuario)
    {
        const string sql = """
            UPDATE usuario

            SET
                tentativas_invalidas = @tentativas,
                bloqueado_ate = @bloqueado

            WHERE id_usuario = @id
            """;

        using var con = Abrir();
        using var cmd =
            new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@tentativas",
            usuario.TentativasInvalidas);

        cmd.Parameters.AddWithValue(
            "@bloqueado",
            (object?)usuario.BloqueadoAte
            ?? DBNull.Value);

        cmd.Parameters.AddWithValue(
            "@id",
            usuario.Id);

        cmd.ExecuteNonQuery();
    }

    public void RegistrarTentativa(
        string emailInformado,
        int? usuarioId,
        bool sucesso,
        string? motivo)
    {
        const string sql = """
            INSERT INTO tentativa_login
            (
                id_usuario,
                email_informado,
                sucesso,
                motivo
            )

            VALUES
            (
                @usuario,
                @email,
                @sucesso,
                @motivo
            )
            """;

        using var con = Abrir();
        using var cmd =
            new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@usuario",
            (object?)usuarioId
            ?? DBNull.Value);

        cmd.Parameters.AddWithValue(
            "@email",
            emailInformado);

        cmd.Parameters.AddWithValue(
            "@sucesso",
            sucesso);

        cmd.Parameters.AddWithValue(
            "@motivo",
            (object?)motivo
            ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public int Inserir(
        string nome,
        string cpf,
        string email,
        string senha,
        Perfil perfil,
        string? telefone = null,
        string? crm = null)
    {
        if (!PoliticaSenha.Valida(
            senha,
            out var erro))
        {
            throw new ArgumentException(
                erro,
                nameof(senha));
        }

        if (perfil == Perfil.Responsavel
            && string.IsNullOrWhiteSpace(
                telefone))
        {
            throw new ArgumentException(
                "Responsável exige telefone.",
                nameof(telefone));
        }

        if (perfil == Perfil.Medico
            && string.IsNullOrWhiteSpace(
                crm))
        {
            throw new ArgumentException(
                "Médico exige CRM.",
                nameof(crm));
        }

        var (hash, salt, iteracoes) =
            HashSenha.Gerar(senha);

        try
        {
            return InserirComTransacao(
                nome,
                cpf,
                email,
                hash,
                salt,
                iteracoes,
                perfil,
                telefone,
                crm);
        }
        catch (MySqlException e)
            when (
                e.ErrorCode
                == MySqlErrorCode.DuplicateKeyEntry)
        {
            var campo =
                e.Message.Contains(
                    "cpf",
                    StringComparison.OrdinalIgnoreCase)
                        ? "CPF"
                        : "e-mail";

            throw new CadastroDuplicadoException(
                campo);
        }
    }

    private int InserirComTransacao(
        string nome,
        string cpf,
        string email,
        byte[] hash,
        byte[] salt,
        int iteracoes,
        Perfil perfil,
        string? telefone,
        string? crm)
    {
        using var con = Abrir();
        using var tx =
            con.BeginTransaction();

        using var cmdUsuario =
            new MySqlCommand(
                """
                INSERT INTO usuario
                (
                    nome,
                    cpf,
                    email,
                    senha_hash,
                    senha_salt,
                    senha_iteracoes,
                    ativo,
                    email_verificado,
                    email_verificado_em
                )

                VALUES
                (
                    @nome,
                    @cpf,
                    @email,
                    @hash,
                    @salt,
                    @iteracoes,
                    1,
                    1,
                    CURRENT_TIMESTAMP
                )
                """,
                con,
                tx);

        cmdUsuario.Parameters.AddWithValue(
            "@nome",
            nome);

        cmdUsuario.Parameters.AddWithValue(
            "@cpf",
            cpf);

        cmdUsuario.Parameters.AddWithValue(
            "@email",
            email);

        cmdUsuario.Parameters.AddWithValue(
            "@hash",
            hash);

        cmdUsuario.Parameters.AddWithValue(
            "@salt",
            salt);

        cmdUsuario.Parameters.AddWithValue(
            "@iteracoes",
            iteracoes);

        cmdUsuario.ExecuteNonQuery();

        var idUsuario =
            (int)cmdUsuario.LastInsertedId;

        var sqlPerfil =
            perfil switch
            {
                Perfil.Responsavel =>
                    """
                    INSERT INTO responsavel
                    (
                        telefone,
                        id_usuario
                    )

                    VALUES
                    (
                        @telefone,
                        @id
                    )
                    """,

                Perfil.Medico =>
                    """
                    INSERT INTO medico
                    (
                        crm,
                        id_usuario
                    )

                    VALUES
                    (
                        @crm,
                        @id
                    )
                    """,

                Perfil.Recepcionista =>
                    """
                    INSERT INTO recepcionista
                    (
                        id_usuario
                    )

                    VALUES
                    (
                        @id
                    )
                    """,

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(perfil))
            };

        using var cmdPerfil =
            new MySqlCommand(
                sqlPerfil,
                con,
                tx);

        cmdPerfil.Parameters.AddWithValue(
            "@id",
            idUsuario);

        cmdPerfil.Parameters.AddWithValue(
            "@telefone",
            (object?)telefone
            ?? DBNull.Value);

        cmdPerfil.Parameters.AddWithValue(
            "@crm",
            (object?)crm
            ?? DBNull.Value);

        cmdPerfil.ExecuteNonQuery();

        tx.Commit();

        return idUsuario;
    }

    public bool ExisteEmail(
        string email)
    {
        return Contar(
            """
            SELECT COUNT(*)
            FROM usuario
            WHERE email = @valor
            """,
            email);
    }

    public bool ExisteEmailDeOutroUsuario(
    string email,
    int usuarioId)
    {
        const string sql = """
        SELECT COUNT(*)
        FROM usuario
        WHERE email = @email
          AND id_usuario <> @id
        """;

        using var con = Abrir();
        using var cmd =
            new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@email",
            email);

        cmd.Parameters.AddWithValue(
            "@id",
            usuarioId);

        return Convert.ToInt32(
            cmd.ExecuteScalar()) > 0;
    }

    public bool ExisteCpf(
        string cpf)
    {
        return Contar(
            """
            SELECT COUNT(*)
            FROM usuario
            WHERE cpf = @valor
            """,
            cpf);
    }

    private bool Contar(
        string sql,
        string valor)
    {
        using var con = Abrir();
        using var cmd =
            new MySqlCommand(sql, con);

        cmd.Parameters.AddWithValue(
            "@valor",
            valor);

        return Convert.ToInt32(
            cmd.ExecuteScalar()) > 0;
    }

    private static Perfil ParaPerfil(
        string nome)
    {
        return nome switch
        {
            "RESPONSAVEL" =>
                Perfil.Responsavel,

            "MEDICO" =>
                Perfil.Medico,

            "RECEPCIONISTA" =>
                Perfil.Recepcionista,

            _ =>
                throw new InvalidOperationException(
                    $"Perfil desconhecido: {nome}")
        };
    }
}