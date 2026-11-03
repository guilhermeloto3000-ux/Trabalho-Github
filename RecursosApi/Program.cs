// Program.cs — VERSÃO FINAL (até a Aula 9 de Web e a Aula 8 de Aplicada)
// Marcas usadas nos comentários:
//   AULA 7          = Aula 7 de Web (Problem Details, validação, paginação)
//   APLICADA 6      = fila de análises e worker
//   WEB 9           = Identity, JWT, autenticação e autorização
//   APLICADA 8      = SignalR (tempo real)
using Microsoft.AspNetCore.Authentication.JwtBearer;    // WEB 9 — NOVO
using Microsoft.AspNetCore.Identity;                     // WEB 9 — NOVO
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;                    // WEB 9 — NOVO

var builder = WebApplication.CreateBuilder(args);

const string CorsFrontEndLocal = "FrontEndLocal";

// ---------------------------------------------------------
// Fase de preparação: registrar serviços
// ---------------------------------------------------------

// Habilita o suporte a Controllers (necessário para o RecursosApiController
// funcionar) — sem esta linha, os atributos [ApiController]/[Route] seriam ignorados.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<RecursoService>();

builder.Services.AddDbContext<RecursosApiContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("RecursosDb"))
           .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddScoped<IRecomendadorRepository,
    RecomendadorRepository>();
builder.Services.AddScoped<RecomendacaoService>();

// APLICADA 6 — fila de análises + worker em segundo plano.
builder.Services.AddSingleton(new FilaDeAnalises(capacidade: 5));   // a MESMA fila para quem escreve e quem lê
builder.Services.AddSingleton<StatusDasAnalises>();                // o MESMO status para worker e endpoints
builder.Services.AddHostedService<AnaliseWorker>();                // o ASP.NET inicia o worker junto com a API

// APLICADA 8 — NOVO: registra o SignalR (Hubs, IHubContext, grupos).
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsFrontEndLocal, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();   // APLICADA 8 — NOVO: o cliente SignalR envia credenciais
                                     // na negociação; sem isto o navegador bloqueia.
    });
});

// AULA 7 — NOVO: todo erro sai em Problem Details (application/problem+json).
builder.Services.AddProblemDetails();

// AULA 7 — NOVO: pedido mal escrito (ex.: "capacidade":"abc") responde 400.
// Sem esta linha, em Development, o ASP.NET lança uma exceção nesses casos e
// o UseExceptionHandler a transforma em 500 — mas o erro é do cliente, não da API.
builder.Services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = false);

// WEB 9 — NOVO: Identity — usuários, senhas (com hash) e roles, guardados no RecursosApiContext.
builder.Services.AddIdentityCore<IdentityUser>(opcoes =>
    {
        opcoes.Password.RequiredLength = 8;                          // mínimo de 8 caracteres
        opcoes.Lockout.MaxFailedAccessAttempts = 5;                  // 5 senhas erradas...
        opcoes.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);   // ...bloqueia por 5 min
    })
    .AddRoles<IdentityRole>()                                        // liga as roles
    .AddEntityFrameworkStores<RecursosApiContext>()                  // tabelas AspNet* no nosso banco
    .AddSignInManager();                                             // confere senha + bloqueio no login

// WEB 9 — NOVO: quem monta o token no login.
builder.Services.AddScoped<TokenService>();

// WEB 9 — NOVO: autenticação — "quem é você?". Lê o JWT do cabeçalho Authorization.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.MapInboundClaims = false;     // mantém os nomes curtos das claims: "name", "role"
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = builder.Configuration["Jwt:Emissor"],          // confere quem emitiu
            ValidAudience = builder.Configuration["Jwt:Publico"],        // confere para quem
            IssuerSigningKey = TokenService.CriarChave(builder.Configuration),  // confere a assinatura
            NameClaimType = "name",          // User.Identity.Name lê a claim "name"
            RoleClaimType = "role"           // RequireRole lê as claims "role"
        };
    });

// WEB 9 — NOVO: autorização — "o que você pode fazer?". Uma política com nome.
builder.Services.AddAuthorization(opcoes =>
    opcoes.AddPolicy("SomenteAdministrador", politica => politica.RequireRole("Administrador")));

var app = builder.Build();

// WEB 9 — NOVO: cria as roles e os usuários de teste (só em Development).
if (app.Environment.IsDevelopment())
{
    await SeedDeUsuarios.CriarAsync(app.Services);
}

// ---------------------------------------------------------
// Fase de execução: montar o pipeline, EM ORDEM
// ---------------------------------------------------------

// 1) Exceção primeiro — envolve tudo o que vem depois.
// AULA 7 — MUDOU: o middleware próprio da Aula 3 foi trocado pelo do ASP.NET.
// app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseExceptionHandler();   // exceção não tratada -> 500 em Problem Details
app.UseStatusCodePages();    // erro sem corpo (401, 403, 404, 405...) ganha corpo em Problem Details

// 2) HTTPS e CORS.
app.UseHttpsRedirection();
app.UseCors(CorsFrontEndLocal);

// 3) WEB 9 — NOVO: depois do CORS e antes dos endpoints, nesta ordem.
app.UseAuthentication();     // lê o token e descobre QUEM é o usuário
app.UseAuthorization();      // confere se ele PODE usar o endpoint

// 4) Log por último entre os middlewares — precisa vir depois pra registrar
//    o status final da resposta.
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------------------------------------------------------
// Endpoints: cada grupo no seu arquivo
// ---------------------------------------------------------

// Ativa as rotas por atributo do RecursosApiController.
app.MapControllers();

app.MapAuth();          // WEB 9 — NOVO: AuthEndpoints.cs (login e /auth/eu)
app.MapRecursos();      // WEB 9 — MUDOU: RecursosEndpoints.cs (CRUD + recomendação)
app.MapAnalises();      // VERSÃO FINAL — MUDOU DE LUGAR: AnaliseEndpoints.cs (Aplicada 6)

// APLICADA 8 — NOVO: o endereço onde os navegadores se conectam ao Hub.
app.MapHub<AnaliseHub>("/hubs/analises");

app.Run();
