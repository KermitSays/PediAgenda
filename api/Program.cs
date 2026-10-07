using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PediAgenda.Api.Seguranca;
using PediAgenda.Nucleo;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Servicos;
using PediAgenda.Api.Servicos;

var builder = WebApplication.CreateBuilder(args);

// A senha do banco e a chave do token vêm de variáveis de ambiente, nunca do
// código. Se alguma faltar, a API nem sobe — melhor falhar agora do que na
// primeira requisição.
if (!Configuracao.TemBanco)
{
    Console.Error.WriteLine($"Defina a variável {Configuracao.VariavelAmbiente} antes de subir a API.");
    Console.Error.WriteLine("Exemplo (PowerShell), trocando SUA_SENHA:");
    Console.Error.WriteLine($"  setx {Configuracao.VariavelAmbiente} " +
        "\"Server=localhost;Port=3306;Database=pediagenda;User ID=root;Password=SUA_SENHA;\"");
    return 1;
}

if (!ConfiguracaoToken.ChaveValida)
{
    Console.Error.WriteLine($"Defina a variável {ConfiguracaoToken.VariavelAmbiente} com pelo menos " +
                            $"{ConfiguracaoToken.TamanhoMinimoChave} caracteres. Para gerar uma aleatória (PowerShell):");
    Console.Error.WriteLine("  $b = New-Object byte[] 48; " +
        "[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); " +
        $"setx {ConfiguracaoToken.VariavelAmbiente} ([Convert]::ToBase64String($b))");
    return 1;
}

builder.Services.AddSingleton<IUsuarioRepositorio>(
    _ => new UsuarioRepositorioMySql(Configuracao.StringDeConexao!));
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<CadastroService>();
builder.Services.AddScoped<ConfirmacaoEmailService>();
builder.Services.AddHttpClient<EmailService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<GeradorToken>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        // Mantém os nomes curtos das informações do token (sub, name, role).
        opcoes.MapInboundClaims = false;
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = ConfiguracaoToken.Emissor,
            ValidAudience = ConfiguracaoToken.Publico,
            IssuerSigningKey = ConfiguracaoToken.ChaveDeAssinatura(),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // Sem isto, token ausente ou vencido devolve 401 com corpo vazio. Aqui
        // ele segue o mesmo formato de erro do resto da API.
        opcoes.Events = new JwtBearerEvents
        {
            OnChallenge = async contexto =>
            {
                contexto.HandleResponse();
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await contexto.Response.WriteAsJsonAsync(new ErroApi("NAO_AUTENTICADO",
                    "Sessão expirada ou inválida. Faça login novamente."));
            },
            OnForbidden = async contexto =>
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                await contexto.Response.WriteAsJsonAsync(new ErroApi("SEM_PERMISSAO",
                    "Seu perfil não tem acesso a esta função."));
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Serve para o app conferir se a API está no ar antes de tentar o login.
app.MapGet("/api/saude", () => Results.Ok(new { status = "ok" }));

// RF03 e RF04 — a regra inteira vive no AutenticacaoService, do núcleo.
// Aqui só traduzimos o resultado dela para HTTP e entregamos o token.
app.MapPost("/api/auth/login", (LoginRequisicao req, AutenticacaoService auth, GeradorToken tokens) =>
{
    var resultado = auth.Autenticar(req.Email, req.Senha);

    if (!resultado.Sucesso)
        return Results.Json(new ErroApi(Codigo(resultado.Motivo), resultado.Mensagem),
                            statusCode: Status(resultado.Motivo));

    return Results.Ok(Sessao(resultado.Usuario!, tokens));
});

// Devolve quem é o dono do token. Serve para o app conferir, ao abrir, se o
// token que ele guardou ainda vale — e é a primeira rota que exige login.
app.MapGet("/api/auth/eu", (ClaimsPrincipal usuario) =>
    Results.Ok(new UsuarioLogado(
        int.Parse(usuario.FindFirstValue("sub")!),
        usuario.FindFirstValue("name")!,
        usuario.FindFirstValue("email")!,
        usuario.FindFirstValue("role")!)))
   .RequireAuthorization();

// Conclusão do cadastro de acesso do responsável.
//
// O responsável já foi previamente cadastrado pela clínica.
// Aqui ele informa CPF + código de ativação e escolhe
// o e-mail e a senha que usará no aplicativo.
//
// Esta rota não exige login, porque o responsável ainda
// está justamente criando o acesso dele.
app.MapPost(
    "/api/cadastro/responsavel",
    async (
        CadastroRequisicao req,
        CadastroService cadastro,
        EmailService emailService) =>
    {
        var resultado =
            cadastro.Cadastrar(
                new DadosResponsavel(
                    req.Cpf,
                    req.CodigoAtivacao,
                    req.Email,
                    req.Senha,
                    req.AceiteTermos));

        if (resultado.Sucesso)
        {
            var responsavel =
                resultado.Usuario!;

            try
            {
                await emailService
                    .EnviarConfirmacaoCadastroAsync(
                        responsavel.Email!,
                        responsavel.Nome,
                        resultado.TokenVerificacaoEmail!);
            }
            catch (Exception e)
                when (
                    e is HttpRequestException
                    or TaskCanceledException
                    or InvalidOperationException)
            {
                // O cadastro fica pendente no banco.
                // Como o código da clínica ainda não foi marcado
                // como utilizado, o responsável poderá tentar
                // novamente e receber um novo link.
                Console.Error.WriteLine(
                    $"Falha ao enviar e-mail de confirmação: {e.Message}");

                return Results.Json(
                    new ErroApi(
                        "EMAIL_NAO_ENVIADO",
                        "Não foi possível enviar o e-mail de confirmação. Tente novamente em alguns instantes."),
                    statusCode:
                        StatusCodes.Status502BadGateway);
            }

            // O token verdadeiro não é devolvido ao aplicativo.
            // Ele é enviado exclusivamente por e-mail.
            return Results.Json(
                new
                {
                    mensagem =
                        "Cadastro iniciado. Enviamos um link de confirmação para o e-mail informado.",

                    usuario = new
                    {
                        id = responsavel.Id,
                        nome = responsavel.Nome,
                        email = responsavel.Email
                    }
                },
                statusCode:
                    StatusCodes.Status202Accepted);
        }

        if (resultado.Motivo
            == MotivoRecusa.DadosInvalidos)
        {
            return Results.Json(
                new ErroValidacao(
                    "DADOS_INVALIDOS",
                    resultado.Mensagem,
                    resultado.Campos!),
                statusCode:
                    StatusCodes.Status400BadRequest);
        }

        if (resultado.Motivo
            == MotivoRecusa.ResponsavelNaoEncontrado)
        {
            return Results.Json(
                new ErroApi(
                    "RESPONSAVEL_NAO_ENCONTRADO",
                    resultado.Mensagem),
                statusCode:
                    StatusCodes.Status404NotFound);
        }

        if (resultado.Motivo
            == MotivoRecusa.CodigoAtivacaoInvalido)
        {
            return Results.Json(
                new ErroApi(
                    "CODIGO_ATIVACAO_INVALIDO",
                    resultado.Mensagem),
                statusCode:
                    StatusCodes.Status400BadRequest);
        }

        if (resultado.Motivo
            == MotivoRecusa.EmailJaCadastrado)
        {
            return Results.Json(
                new ErroApi(
                    "EMAIL_JA_CADASTRADO",
                    resultado.Mensagem),
                statusCode:
                    StatusCodes.Status409Conflict);
        }

        if (resultado.Motivo
            == MotivoRecusa.CadastroJaIniciado)
        {
            return Results.Json(
                new ErroApi(
                    "CADASTRO_JA_INICIADO",
                    resultado.Mensagem),
                statusCode:
                    StatusCodes.Status409Conflict);
        }

        return Results.Json(
            new ErroApi(
                "ERRO_CADASTRO",
                resultado.Mensagem),
            statusCode:
                StatusCodes.Status400BadRequest);
    });

// Confirma o e-mail do responsável por meio do token
// enviado no link de verificação.
app.MapGet(
    "/api/cadastro/confirmar-email",
    (
        string? token,
        ConfirmacaoEmailService confirmacao) =>
    {
        var resultado =
            confirmacao.Confirmar(token);

        if (resultado.Sucesso)
        {
            return Results.Ok(
                new
                {
                    mensagem = resultado.Mensagem
                });
        }

        if (resultado.Motivo
            == MotivoFalhaConfirmacaoEmail.TokenAusente)
        {
            return Results.Json(
                new ErroApi(
                    "TOKEN_NAO_INFORMADO",
                    resultado.Mensagem),
                statusCode:
                    StatusCodes.Status400BadRequest);
        }

        return Results.Json(
            new ErroApi(
                "TOKEN_INVALIDO_OU_EXPIRADO",
                resultado.Mensagem),
            statusCode:
                StatusCodes.Status400BadRequest);
    });

app.Run();
return 0;

static SessaoIniciada Sessao(Usuario u, GeradorToken tokens)
{
    var perfil = u.Perfil.ToString().ToUpperInvariant();
    var (token, expiraEm) = tokens.Gerar(u.Id, u.Nome, u.Email, perfil);
    return new SessaoIniciada(u.Id, u.Nome, u.Email, perfil, token, expiraEm);
}

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
record CadastroRequisicao(  string? Cpf, string? CodigoAtivacao, string? Email, string? Senha, bool AceiteTermos);
record UsuarioLogado(int Id, string Nome, string Email, string Perfil);
record SessaoIniciada(int Id, string Nome, string Email, string Perfil, string Token, DateTime ExpiraEm);
record ErroApi(string Codigo, string Mensagem);
record ErroValidacao(string Codigo, string Mensagem, IReadOnlyDictionary<string, string> Campos);
