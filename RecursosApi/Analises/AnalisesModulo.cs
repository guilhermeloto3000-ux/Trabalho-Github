// Analises/AnalisesModulo.cs — AULA FINAL — NOVO
// Registro da fila de análises (Aplicada 6) e do SignalR (Aplicada 8). Veio do Program.cs.
public static class AnalisesModulo
{
    public static IServiceCollection AddAnalises(this IServiceCollection services)
    {
        services.AddSingleton(new FilaDeAnalises(capacidade: 5));   // a MESMA fila para quem escreve e quem lê
        services.AddSingleton<StatusDasAnalises>();                // o MESMO status para worker e endpoints
        services.AddHostedService<AnaliseWorker>();                // o ASP.NET inicia o worker junto com a API
        services.AddSignalR();                                     // Hubs, IHubContext, grupos
        return services;
    }
}
