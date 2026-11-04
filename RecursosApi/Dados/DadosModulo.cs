// Dados/DadosModulo.cs — AULA FINAL — NOVO
// Registro do banco (veio do Program.cs) e a preparação do banco na subida da API.
using Microsoft.EntityFrameworkCore;

public static class DadosModulo
{
    public static IServiceCollection AddDados(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<RecursosApiContext>(options =>
            options.UseSqlite(config.GetConnectionString("RecursosDb"))
                   .LogTo(Console.WriteLine, LogLevel.Information));
        return services;
    }

    // AULA FINAL — NOVO: aplica as migrations que ainda não foram aplicadas.
    // É o mesmo que o "dotnet ef database update" faz, mas executado pela própria API ao subir.
    // No servidor ninguém digita comandos, e no Render o disco é apagado a cada reinício:
    // sem isto, a API subiria com um banco sem tabelas ("no such table").
    // Se o banco já está em dia, Migrate() não faz nada.
    public static async Task AplicarMigrationsAsync(this WebApplication app)
    {
        using var escopo = app.Services.CreateScope();   // o DbContext é Scoped
        var contexto = escopo.ServiceProvider.GetRequiredService<RecursosApiContext>();
        await contexto.Database.MigrateAsync();
    }
}
