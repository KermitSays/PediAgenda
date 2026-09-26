using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using PediAgenda.Api.Seguranca;
using PediAgenda.Nucleo;
using PediAgenda.Nucleo.Modelos;
using PediAgenda.Nucleo.Repositorios;
using PediAgenda.Nucleo.Servicos;

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
builder.Services.AddSingleton<IPacienteRepositorio>(
    _ => new PacienteRepositorioMySql(Configuracao.StringDeConexao!));
builder.Services.AddScoped<PacienteService>();
builder.Services.AddSingleton<IHorarioRepositorio>(
    _ => new HorarioRepositorioMySql(Configuracao.StringDeConexao!));
builder.Services.AddScoped<HorarioService>();
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

// RF01 — cadastro do responsável. Médico e recepcionista são criados pela
// clínica, não por aqui. Já devolve o token: quem acabou de se cadastrar
// entra direto, sem precisar logar de novo.
app.MapPost("/api/cadastro/responsavel", (CadastroRequisicao req, CadastroService cadastro, GeradorToken tokens) =>
{
    var resultado = cadastro.Cadastrar(new DadosResponsavel(
        req.Nome, req.Cpf, req.Email, req.Telefone, req.Senha, req.AceiteTermos));

    if (resultado.Sucesso)
        return Results.Json(Sessao(resultado.Usuario!, tokens),
                            statusCode: StatusCodes.Status201Created);

    if (resultado.Motivo == MotivoRecusa.DadosInvalidos)
        return Results.Json(new ErroValidacao("DADOS_INVALIDOS", resultado.Mensagem, resultado.Campos!),
                            statusCode: StatusCodes.Status400BadRequest);

    var codigo = resultado.Motivo == MotivoRecusa.CpfJaCadastrado
        ? "CPF_JA_CADASTRADO"
        : "EMAIL_JA_CADASTRADO";
    return Results.Json(new ErroApi(codigo, resultado.Mensagem),
                        statusCode: StatusCodes.Status409Conflict);
});

// Pacientes (as crianças) do responsável logado. Só o perfil RESPONSAVEL
// entra aqui, e o responsável sai do token — não há id na URL para trocar.
var pacientes = app.MapGroup("/api/pacientes")
                   .RequireAuthorization(politica => politica.RequireRole("RESPONSAVEL"));

pacientes.MapGet("", (ClaimsPrincipal usuario, PacienteService servico) =>
{
    var lista = servico.Listar(IdUsuario(usuario));
    if (lista is null)
        return Results.Json(new ErroApi("SEM_PERMISSAO", "Só o responsável pode ver pacientes."),
                            statusCode: StatusCodes.Status403Forbidden);

    return Results.Ok(lista.Select(PacienteResposta.De));
});

pacientes.MapPost("", (PacienteRequisicao req, ClaimsPrincipal usuario, PacienteService servico) =>
{
    var resultado = servico.Cadastrar(IdUsuario(usuario), req.Nome, req.DataNascimento);

    return resultado.Motivo switch
    {
        MotivoRecusaPaciente.Nenhum =>
            Results.Json(PacienteResposta.De(resultado.Paciente!), statusCode: StatusCodes.Status201Created),
        MotivoRecusaPaciente.DadosInvalidos =>
            Results.Json(new ErroValidacao("DADOS_INVALIDOS", resultado.Mensagem, resultado.Campos!),
                         statusCode: StatusCodes.Status400BadRequest),
        MotivoRecusaPaciente.JaCadastrado =>
            Results.Json(new ErroApi("PACIENTE_JA_CADASTRADO", resultado.Mensagem),
                         statusCode: StatusCodes.Status409Conflict),
        _ =>
            Results.Json(new ErroApi("SEM_PERMISSAO", resultado.Mensagem),
                         statusCode: StatusCodes.Status403Forbidden)
    };
});

// RF05 e RNF03 — horários livres de um dia, agrupados por médico. Qualquer
// perfil logado consulta: o responsável para agendar, a recepção para atender
// quem liga.
app.MapGet("/api/horarios", (string? data, string? especialidade, int? medico, HorarioService servico) =>
{
    var resultado = servico.Livres(data, especialidade, medico);

    if (!resultado.Sucesso)
        return Results.Json(new ErroValidacao("DADOS_INVALIDOS", "Verifique os filtros.", resultado.Campos!),
                            statusCode: StatusCodes.Status400BadRequest);

    var medicos = resultado.Livres
        .GroupBy(h => (h.IdMedico, h.NomeMedico, h.Especialidade))
        .Select(grupo => new MedicoComHorarios(
            grupo.Key.IdMedico, grupo.Key.NomeMedico, grupo.Key.Especialidade,
            grupo.Select(h => new HorarioResposta(
                h.IdHorario, h.Inicio.ToString("HH:mm"), h.Fim.ToString("HH:mm"))).ToList()))
        .ToList();

    return Results.Ok(new HorariosDoDia(resultado.Data.ToString("yyyy-MM-dd"), medicos));
}).RequireAuthorization();

app.Run();
return 0;

static int IdUsuario(ClaimsPrincipal usuario) => int.Parse(usuario.FindFirstValue("sub")!);

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
record CadastroRequisicao(string? Nome, string? Cpf, string? Email,
                          string? Telefone, string? Senha, bool AceiteTermos);
record UsuarioLogado(int Id, string Nome, string Email, string Perfil);
record SessaoIniciada(int Id, string Nome, string Email, string Perfil, string Token, DateTime ExpiraEm);
record ErroApi(string Codigo, string Mensagem);
record ErroValidacao(string Codigo, string Mensagem, IReadOnlyDictionary<string, string> Campos);

record HorariosDoDia(string Data, IReadOnlyList<MedicoComHorarios> Medicos);
record MedicoComHorarios(int Id, string Nome, string Especialidade, IReadOnlyList<HorarioResposta> Horarios);
record HorarioResposta(int Id, string Inicio, string Fim);

record PacienteRequisicao(string? Nome, string? DataNascimento);
record PacienteResposta(int Id, string Nome, string DataNascimento, int Idade)
{
    public static PacienteResposta De(Paciente p) =>
        new(p.Id, p.Nome, p.DataNascimento.ToString("yyyy-MM-dd"),
            p.IdadeEm(DateOnly.FromDateTime(DateTime.Today)));
}
