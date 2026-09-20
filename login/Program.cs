using System.Text;
using PediAgenda.Nucleo;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Servicos;

Console.OutputEncoding = Encoding.UTF8;

var comando = args.Length > 0 ? args[0].ToLowerInvariant() : "demo";

if (comando == "criar-usuario")
{
    CriarUsuarioDeTeste();
    return;
}

if (comando == "testar-hash")
{
    TestarHash();
    return;
}

Console.WriteLine("PediAgenda - Módulo Login (H2, Sprint 1)");
Console.WriteLine("Demonstração dos requisitos RF03, RF04, RNF01 e RNF02.");

// Mesma regra de autenticação nos dois modos: só muda o repositório.
IUsuarioRepositorio repo;
UsuarioRepositorioMemoria? memoria = null;

if (Configuracao.TemBanco)
{
    Console.WriteLine("Origem dos dados: MySQL (pediagenda)\n");
    repo = new UsuarioRepositorioMySql(Configuracao.StringDeConexao!);
}
else
{
    Console.WriteLine($"Origem dos dados: memória — defina {Configuracao.VariavelAmbiente} para usar o MySQL\n");
    memoria = new UsuarioRepositorioMemoria();
    repo = memoria;
}

var auth = new AutenticacaoService(repo);
Console.WriteLine(new string('=', 74));

void Cenario(string titulo, string email, string senha, int vezes = 1)
{
    Console.WriteLine($"\n> {titulo}");
    for (var i = 0; i < vezes; i++)
    {
        var r = auth.Autenticar(email, senha);
        var etiqueta = r.Sucesso ? "OK  " : "ERRO";
        var sufixo = vezes > 1 ? $" (tentativa {i + 1})" : "";
        Console.WriteLine($"  [{etiqueta}] {r.Mensagem}{sufixo}");
    }
}

var emailValido = Configuracao.TemBanco ? "teste@pediagenda.local" : "ana.responsavel@pediagenda.local";
var senhaValida = Configuracao.TemBanco ? "senhaTeste123" : "minhaSenha88";
var emailBloqueio = Configuracao.TemBanco ? "teste@pediagenda.local" : "joab@pediagenda.local";

Cenario("RF03 - login válido", emailValido, senhaValida);
Cenario("RNF01 - senha com menos de 8 caracteres é barrada antes de consultar a base",
        emailValido, "curta");
Cenario("RF04 - e-mail inexistente (mensagem genérica, não revela cadastros)",
        "ninguem@pediagenda.local", "senhaQualquer1");
Cenario("RF04 - senha incorreta", emailValido, "senhaErrada123");
Cenario("RNF02 - cinco tentativas inválidas bloqueiam a conta",
        emailBloqueio, "senhaErrada123", vezes: 5);
Cenario("RNF02 - conta bloqueada recusa até a senha correta", emailBloqueio, senhaValida);

Console.WriteLine("\n" + new string('=', 74));

if (memoria is not null)
{
    Console.WriteLine("Trilha de auditoria (equivale à tabela tentativa_login):\n");
    foreach (var linha in memoria.Auditoria)
        Console.WriteLine("  " + linha);
}
else
{
    Console.WriteLine("Trilha de auditoria gravada na tabela tentativa_login. Para consultar:\n");
    Console.WriteLine("  SELECT ocorrido_em, email_informado, sucesso, motivo");
    Console.WriteLine("  FROM tentativa_login ORDER BY id DESC LIMIT 20;");
    Console.WriteLine("\nO usuário de teste ficou bloqueado por 15 minutos. Para liberar agora:\n");
    Console.WriteLine("  UPDATE usuario SET bloqueado_ate = NULL, tentativas_invalidas = 0");
    Console.WriteLine($"  WHERE email = '{emailValido}';");
}

void TestarHash()
{
    const string senha = "senhaForte123";
    Console.WriteLine("PediAgenda - Demonstração do armazenamento de senhas (RNF01)");
    Console.WriteLine(new string('=', 74));

    // 1) A mesma senha gera hashes diferentes por causa do salt individual.
    Console.WriteLine($"\n1. Dois usuários com a MESMA senha ({senha}):\n");
    for (var i = 1; i <= 2; i++)
    {
        var (h, s, _) = PediAgenda.Nucleo.Seguranca.HashSenha.Gerar(senha);
        Console.WriteLine($"   usuário {i}:  salt {Convert.ToHexString(s)[..16]}...  " +
                          $"hash {Convert.ToHexString(h)[..32]}...");
    }
    Console.WriteLine("\n   Hashes diferentes: é o salt aleatório de cada usuário.");

    // 2) Conferência: é assim que o login valida, sem nunca ler a senha guardada.
    Console.WriteLine("\n2. Conferência no login:\n");
    var (hash, salt, iteracoes) = PediAgenda.Nucleo.Seguranca.HashSenha.Gerar(senha);
    foreach (var tentativa in new[] { "senhaForte123", "senhaForte124", "SenhaForte123" })
    {
        var ok = PediAgenda.Nucleo.Seguranca.HashSenha.Conferir(tentativa, hash, salt, iteracoes);
        Console.WriteLine($"   digitou \"{tentativa}\"".PadRight(35) +
                          (ok ? "-> CONFERE" : "-> não confere"));
    }

    // 3) A lentidão é proposital: atrasa quem tenta adivinhar por força bruta.
    Console.WriteLine("\n3. Por que 100.000 iterações:\n");
    var sw = System.Diagnostics.Stopwatch.StartNew();
    PediAgenda.Nucleo.Seguranca.HashSenha.Gerar(senha);
    var lento = sw.Elapsed.TotalMilliseconds;

    sw.Restart();
    System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(senha));
    var rapido = sw.Elapsed.TotalMilliseconds;

    Console.WriteLine($"   SHA-256 uma única vez : {rapido * 1000,9:F1} microssegundos");
    Console.WriteLine($"   PBKDF2 100.000 vezes  : {lento,9:F1} milissegundos");
    Console.WriteLine($"\n   O PBKDF2 é cerca de {lento / Math.Max(rapido, 0.0001):N0}x mais lento.");
    Console.WriteLine("   Imperceptível para quem faz login uma vez;");
    Console.WriteLine("   multiplica por esse fator o tempo de quem testa milhões de senhas.");

    Console.WriteLine("\n" + new string('=', 74));
    Console.WriteLine("Em nenhum momento a senha em texto puro é gravada ou recuperada.");
}

void CriarUsuarioDeTeste()
{
    if (!Configuracao.TemBanco)
    {
        Console.WriteLine($"Defina a variável {Configuracao.VariavelAmbiente} antes de criar o usuário.");
        Console.WriteLine("Exemplo (PowerShell), trocando SUA_SENHA:");
        Console.WriteLine($"  setx {Configuracao.VariavelAmbiente} " +
                          "\"Server=localhost;Port=3306;Database=pediagenda;User ID=root;Password=SUA_SENHA;\"");
        return;
    }

    var banco = new UsuarioRepositorioMySql(Configuracao.StringDeConexao!);
    const string email = "teste@pediagenda.local";

    if (banco.Existe(email))
    {
        Console.WriteLine($"Usuário {email} já existe. Nada a fazer.");
        return;
    }

    var id = banco.Inserir("Usuario de Teste", "00000000191", email, "senhaTeste123", Perfil.Responsavel,
                           telefone: "11999990000");
    Console.WriteLine($"Usuário criado com id {id}: {email} / senhaTeste123");
    Console.WriteLine("Agora rode 'dotnet run' para autenticar contra o banco.");
}
