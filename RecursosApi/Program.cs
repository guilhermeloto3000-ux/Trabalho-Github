// Program.cs — AULA FINAL (modularizado e pronto para publicar)
// O que mudou nesta aula:
//   1) Os registros de serviços foram para os módulos: cada pasta tem um XxxModulo.cs
//      com um método AddXxx(). O código de cada registro é o mesmo de antes.
//   2) A API prepara o próprio banco ao subir (migrations + roles + usuários do seed).
//   3) A API também entrega o React publicado (pasta wwwroot): um endereço só.
//   4) GET /health para o Render saber se a API está de pé.
// O pipeline (os app.Use...) continua aqui, porque a ORDEM importa e precisa ficar visível.

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------
// Fase de preparação: registrar serviços — um módulo por assunto
// ---------------------------------------------------------
builder.Services.AddInfraestrutura();                       // Infraestrutura/: Controllers, Swagger, CORS, Problem Details, health
builder.Services.AddDados(builder.Configuration);           // Dados/: DbContext com SQLite
builder.Services.AddAutenticacao(builder.Configuration);    // Autenticacao/: Identity, JWT, política SomenteAdministrador
builder.Services.AddRecursos();                             // Recursos/: RecursoService
builder.Services.AddRecomendacao();                         // Recomendacao/: repositório e serviço
builder.Services.AddAnalises();                             // Analises/: fila, worker e SignalR

var app = builder.Build();

// AULA FINAL — MUDOU: antes, só o seed rodava, e só em Development.
// Agora roda em Development E em Production (no servidor ninguém digita comandos).
// Nos testes (ambiente "Testing"), quem prepara o banco é a RecursosApiFactory.
if (!app.Environment.IsEnvironment("Testing"))
{
    await app.AplicarMigrationsAsync();             // Dados/DadosModulo.cs
    await SeedDeUsuarios.CriarAsync(app.Services);  // Autenticacao/SeedDeUsuarios.cs
}

// ---------------------------------------------------------
// Fase de execução: montar o pipeline, EM ORDEM
// ---------------------------------------------------------

// 1) Exceção primeiro — envolve tudo o que vem depois.
app.UseExceptionHandler();   // exceção não tratada -> 500 em Problem Details
app.UseStatusCodePages();    // erro sem corpo (401, 403, 404...) ganha corpo em Problem Details

// 2) AULA FINAL — NOVO: entrega os arquivos do React publicado (wwwroot/index.html, .js, .css).
//    Em desenvolvimento a pasta wwwroot não existe e quem entrega o React é o Vite.
app.UseStaticFiles();

// 3) CORS. AULA FINAL — MUDOU: saiu o UseHttpsRedirection. Localmente a API só tem http,
//    e no Render o HTTPS é feito pelo próprio Render antes de o pedido chegar na API.
app.UseCors(InfraestruturaModulo.CorsFrontEndLocal);

// 4) Depois do CORS e antes dos endpoints, nesta ordem.
app.UseAuthentication();     // lê o token e descobre QUEM é o usuário
app.UseAuthorization();      // confere se ele PODE usar o endpoint

// 5) Log por último entre os middlewares — registra o status final da resposta.
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ---------------------------------------------------------
// Endpoints: cada grupo no seu módulo — as rotas REST são as mesmas de antes
// ---------------------------------------------------------
app.MapControllers();        // Recursos/RecursosApiController.cs (api/recursos-mvc)
app.MapAuth();               // Autenticacao/AuthEndpoints.cs   (/auth/...)
app.MapRecursos();           // Recursos/RecursosEndpoints.cs    (/recursos/...)
app.MapAnalises();           // Analises/AnaliseEndpoints.cs     (/recursos/{id}/analise, /fila/status, /hubs/analises)

app.MapHealthChecks("/health");   // AULA FINAL — NOVO

// AULA FINAL — NOVO: qualquer GET que não seja da API nem um arquivo (ex.: /login, /cadastro)
// devolve o index.html. Assim o React Router abre a tela certa quando o usuário
// recarrega a página ou abre o link direto. Fica por último: só vale se nada acima respondeu.
app.MapFallbackToFile("index.html");

app.Run();
