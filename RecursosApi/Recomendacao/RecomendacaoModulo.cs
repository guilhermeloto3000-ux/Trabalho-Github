// Recomendacao/RecomendacaoModulo.cs — AULA FINAL — NOVO
// Registro dos serviços da recomendação (Aulas 1 a 5 de Aplicada). Veio do Program.cs.
public static class RecomendacaoModulo
{
    public static IServiceCollection AddRecomendacao(this IServiceCollection services)
    {
        services.AddScoped<IRecomendadorRepository, RecomendadorRepository>();
        services.AddScoped<RecomendacaoService>();
        return services;
    }
}
