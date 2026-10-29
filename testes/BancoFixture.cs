using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;
using PediAgenda.Nucleo;
using PediAgenda.Nucleo.Seguranca;
using Xunit;

namespace PediAgenda.Testes;

[CollectionDefinition("MySQL", DisableParallelization = true)]
public class BancoCollection : ICollectionFixture<BancoFixture> { }

// Cria e remove SOMENTE um banco novo de nome aleatório. Nunca importa o
// schema no banco da aplicação e não usa PEDIAGENDA_CONEXAO como origem.
public sealed class BancoFixture : IDisposable
{
    private readonly string administracao;
    private readonly string nome = "pediagenda_test_" + Guid.NewGuid().ToString("N");
    private readonly string? conexaoAnterior = Environment.GetEnvironmentVariable("PEDIAGENDA_CONEXAO");
    private readonly string? chaveAnterior = Environment.GetEnvironmentVariable("PEDIAGENDA_JWT_CHAVE");
    private WebApplicationFactory<Program>? factory;
    public string Conexao { get; }
    public DateOnly Hoje => DateOnly.FromDateTime(RelogioClinica.Instancia.GetLocalNow().DateTime);
    public BancoFixture()
    {
        var origem = Environment.GetEnvironmentVariable("PEDIAGENDA_TEST_CONEXAO")
            ?? throw new InvalidOperationException("Defina PEDIAGENDA_TEST_CONEXAO para um MySQL de testes com permissão CREATE DATABASE.");
        var builder = new MySqlConnectionStringBuilder(origem) { Database = "" };
        administracao = builder.ConnectionString;
        using (var con = new MySqlConnection(administracao))
        {
            con.Open();
            new MySqlCommand($"CREATE DATABASE {nome} CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci", con).ExecuteNonQuery();
        }
        builder.Database = nome;
        Conexao = builder.ConnectionString;
        using (var con = new MySqlConnection(Conexao))
        {
            con.Open();
            var schema = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "01_Schema.sql"));
            schema = Regex.Replace(schema, @"CREATE DATABASE IF NOT EXISTS pediagenda\s+CHARACTER SET utf8mb4\s+COLLATE utf8mb4_0900_ai_ci;", "");
            schema = schema.Replace("USE pediagenda;", "");
            new MySqlCommand(schema, con).ExecuteNonQuery();
        }
        Environment.SetEnvironmentVariable("PEDIAGENDA_CONEXAO", Conexao);
        Environment.SetEnvironmentVariable("PEDIAGENDA_JWT_CHAVE", new string('t', 64));
    }

    public void Reiniciar()
    {
        using var con = new MySqlConnection(Conexao);
        con.Open();
        new MySqlCommand("""
            DELETE FROM tentativa_login; DELETE FROM relatorio_atendimento;
            DELETE FROM notificacao; DELETE FROM prontuario; DELETE FROM consulta;
            DELETE FROM horario; DELETE FROM agenda_medica; DELETE FROM paciente;
            DELETE FROM medico; DELETE FROM recepcionista; DELETE FROM responsavel; DELETE FROM usuario;
            """, con).ExecuteNonQuery();
        var (hash, salt, iteracoes) = HashSenha.Gerar("senhaDemo123");
        for (int i = 1; i <= 4; i++)
        {
            using var cmd = new MySqlCommand("""
                INSERT INTO usuario (id_usuario,nome,cpf,email,senha_hash,senha_salt,senha_iteracoes,ativo)
                VALUES (@id,@nome,@cpf,@email,@hash,@salt,@iteracoes,TRUE)
                """, con);
            cmd.Parameters.AddWithValue("@id", i);
            cmd.Parameters.AddWithValue("@nome", "Usuário " + i);
            cmd.Parameters.AddWithValue("@cpf", i.ToString("D11"));
            cmd.Parameters.AddWithValue("@email", $"usuario{i}@teste.local");
            cmd.Parameters.AddWithValue("@hash", hash);
            cmd.Parameters.AddWithValue("@salt", salt);
            cmd.Parameters.AddWithValue("@iteracoes", iteracoes);
            cmd.ExecuteNonQuery();
        }
        using var seed = new MySqlCommand("""
            INSERT INTO medico VALUES (1,'CRM1','Pediatria',1),(2,'CRM2','Pediatria',2);
            INSERT INTO responsavel VALUES (1,'11999999999',3);
            INSERT INTO recepcionista VALUES (1,4);
            INSERT INTO paciente VALUES (1,'Paciente A','2020-01-01',1),(2,'Paciente B','2021-01-01',1);
            INSERT INTO agenda_medica VALUES (1,1),(2,2);
            INSERT INTO horario VALUES
                (1,@ontem,'10:00','10:30',TRUE,1),
                (2,@ontem,'11:00','11:30',TRUE,1),
                (3,@ontem,'12:00','12:30',TRUE,2),
                (4,@ontem,'13:00','13:30',TRUE,1),
                (10,@amanha,'10:00','10:30',TRUE,1),
                (11,@amanha,'10:30','11:00',TRUE,1),
                (12,@amanha,'11:00','11:30',TRUE,1),
                (13,@depois,'09:00','09:30',TRUE,1),
                (14,@amanha,'14:00','14:30',TRUE,1),
                (20,@amanha,'10:00','10:30',TRUE,2);
            INSERT INTO consulta (id_consulta,status,tipo_atendimento,id_paciente,id_horario) VALUES
                (1,'AGENDADA','PARTICULAR',1,1),
                (2,'CONFIRMADA','CONVENIO',1,2),
                (3,'AGENDADA','PARTICULAR',2,3),
                (4,'CANCELADA','PARTICULAR',1,4),
                (10,'AGENDADA','PARTICULAR',1,10),
                (11,'CONFIRMADA','CONVENIO',1,11);
            """, con);
        seed.Parameters.AddWithValue("@ontem", Hoje.AddDays(-1));
        seed.Parameters.AddWithValue("@amanha", Hoje.AddDays(1));
        seed.Parameters.AddWithValue("@depois", Hoje.AddDays(2));
        seed.ExecuteNonQuery();
    }

    public HttpClient Cliente()
    {
        factory ??= new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.UseContentRoot(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../api"))));
        return factory.CreateClient();
    }
    public async Task<HttpClient> Autenticado(int usuario)
    {
        var client = Cliente();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = $"usuario{usuario}@teste.local", senha = "senhaDemo123" });
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("token").GetString());
        return client;
    }
    public object? Escalar(string sql)
    {
        using var con = new MySqlConnection(Conexao); con.Open();
        return new MySqlCommand(sql, con).ExecuteScalar();
    }
    public void Executar(string sql)
    {
        using var con = new MySqlConnection(Conexao); con.Open();
        new MySqlCommand(sql, con).ExecuteNonQuery();
    }
    public void Dispose()
    {
        factory?.Dispose();
        Environment.SetEnvironmentVariable("PEDIAGENDA_CONEXAO", conexaoAnterior);
        Environment.SetEnvironmentVariable("PEDIAGENDA_JWT_CHAVE", chaveAnterior);
        MySqlConnection.ClearAllPools();
        using var con = new MySqlConnection(administracao); con.Open();
        new MySqlCommand($"DROP DATABASE {nome}", con).ExecuteNonQuery();
    }
}
