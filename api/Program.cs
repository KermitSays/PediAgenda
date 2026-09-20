using PediAgenda.Nucleo;
using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Servicos;

var builder = WebApplication.CreateBuilder(args);

// A senha do banco vem da variável de ambiente PEDIAGENDA_CONEXAO, nunca do
// código. Se ela não existir, a API nem sobe — melhor falhar agora do que na
// primeira tentativa de login.
if (!Configuracao.TemBanco)
{
    Console.Error.WriteLine($"Defina a variável {Configuracao.VariavelAmbiente} antes de subir a API.");
    Console.Error.WriteLine("Exemplo (PowerShell), trocando SUA_SENHA:");
    Console.Error.WriteLine($"  setx {Configuracao.VariavelAmbiente} " +
        "\"Server=localhost;Port=3306;Database=pediagenda;User ID=root;Password=SUA_SENHA;\"");
    return 1;
}

builder.Services.AddSingleton<IUsuarioRepositorio>(
    _ => new UsuarioRepositorioMySql(Configuracao.StringDeConexao!));
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<CadastroService>();

var app = builder.Build();

// Serve para o app conferir se a API está no ar antes de tentar o login.
app.MapGet("/api/saude", () => Results.Ok(new { status = "ok" }));

// RF03 e RF04 — a regra inteira vive no AutenticacaoService, do núcleo.
// Aqui só traduzimos o resultado dela para HTTP.
app.MapPost("/api/auth/login", (LoginRequisicao req, AutenticacaoService auth) =>
{
    var resultado = auth.Autenticar(req.Email, req.Senha);

    if (!resultado.Sucesso)
        return Results.Json(new ErroApi(Codigo(resultado.Motivo), resultado.Mensagem),
                            statusCode: Status(resultado.Motivo));

    var u = resultado.Usuario!;
    return Results.Ok(new UsuarioLogado(u.Id, u.Nome, u.Email,
                                        u.Perfil.ToString().ToUpperInvariant()));
});

// RF01 — cadastro do responsável. Médico e recepcionista são criados pela
// clínica, não por aqui.
app.MapPost("/api/cadastro/responsavel", (CadastroRequisicao req, CadastroService cadastro) =>
{
    var resultado = cadastro.Cadastrar(new DadosResponsavel(
        req.Nome, req.Cpf, req.Email, req.Telefone, req.Senha, req.AceiteTermos));

    if (resultado.Sucesso)
    {
        var novo = resultado.Usuario!;
        return Results.Json(new UsuarioLogado(novo.Id, novo.Nome, novo.Email,
                                              novo.Perfil.ToString().ToUpperInvariant()),
                            statusCode: StatusCodes.Status201Created);
    }

    if (resultado.Motivo == MotivoRecusa.DadosInvalidos)
        return Results.Json(new ErroValidacao("DADOS_INVALIDOS", resultado.Mensagem, resultado.Campos!),
                            statusCode: StatusCodes.Status400BadRequest);

    var codigo = resultado.Motivo == MotivoRecusa.CpfJaCadastrado
        ? "CPF_JA_CADASTRADO"
        : "EMAIL_JA_CADASTRADO";
    return Results.Json(new ErroApi(codigo, resultado.Mensagem),
                        statusCode: StatusCodes.Status409Conflict);
});

app.Run();
return 0;

static int Status(MotivoFalha motivo) => motivo switch
{
    MotivoFalha.SenhaForaDaPolitica => StatusCodes.Status400BadRequest,
    MotivoFalha.CredenciaisInvalidas => StatusCodes.Status401Unauthorized,
    MotivoFalha.ContaInativa => StatusCodes.Status403Forbidden,
    MotivoFalha.ContaBloqueada => StatusCodes.Status423Locked,
    _ => StatusCodes.Status400BadRequest
};

static string Codigo(MotivoFalha motivo) => motivo switch
{
    MotivoFalha.SenhaForaDaPolitica => "SENHA_FORA_DA_POLITICA",
    MotivoFalha.CredenciaisInvalidas => "CREDENCIAIS_INVALIDAS",
    MotivoFalha.ContaInativa => "CONTA_INATIVA",
    MotivoFalha.ContaBloqueada => "CONTA_BLOQUEADA",
    _ => "ERRO"
};

// O que a tela manda e o que ela recebe. A senha só entra; nunca sai.
record LoginRequisicao(string? Email, string? Senha);
record CadastroRequisicao(string? Nome, string? Cpf, string? Email,
                          string? Telefone, string? Senha, bool AceiteTermos);
record UsuarioLogado(int Id, string Nome, string Email, string Perfil);
record ErroApi(string Codigo, string Mensagem);
record ErroValidacao(string Codigo, string Mensagem, IReadOnlyDictionary<string, string> Campos);
