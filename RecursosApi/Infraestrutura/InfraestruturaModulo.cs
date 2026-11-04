// Infraestrutura/InfraestruturaModulo.cs — AULA FINAL — NOVO
// O que não pertence a um assunto só: Controllers, Swagger, CORS, Problem Details e /health.
// Veio do Program.cs (menos o AddHealthChecks, que é novo).
public static class InfraestruturaModulo
{
    public const string CorsFrontEndLocal = "FrontEndLocal";

    public static IServiceCollection AddInfraestrutura(this IServiceCollection services)
    {
        // Habilita o suporte a Controllers (necessário para o RecursosApiController).
        services.AddControllers();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // CORS para o Vite em http://localhost:5173.
        // AULA FINAL: com o proxy do Vite (vite.config.js) o navegador fala só com a 5173,
        // e em produção a página e a API têm o MESMO endereço. Nos dois casos não há
        // pedido de outra origem; a política fica para quem abrir o front de outro endereço.
        services.AddCors(options =>
        {
            options.AddPolicy(CorsFrontEndLocal, policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();   // o cliente SignalR envia credenciais na negociação
            });
        });

        // Todo erro sai em Problem Details (application/problem+json).
        services.AddProblemDetails();

        // Pedido mal escrito (ex.: "capacidade":"abc") responde 400, e não 500.
        services.Configure<RouteHandlerOptions>(opcoes => opcoes.ThrowOnBadRequest = false);

        // AULA FINAL — NOVO: verificação de saúde. GET /health responde 200 "Healthy"
        // enquanto a API está de pé. O Render chama esse endereço depois de cada deploy
        // e só troca para a versão nova quando ela responde.
        services.AddHealthChecks();

        return services;
    }
}
