// Recursos/RecursosModulo.cs — AULA FINAL — NOVO
// Cada pasta (módulo) registra os PRÓPRIOS serviços num método de extensão.
// O Program.cs só chama builder.Services.AddRecursos(): ele não precisa saber
// quais classes existem dentro da pasta Recursos.
// O código de registro é o MESMO que estava no Program.cs; só mudou de lugar.
public static class RecursosModulo
{
    public static IServiceCollection AddRecursos(this IServiceCollection services)
    {
        services.AddScoped<RecursoService>();
        return services;   // devolver o services permite encadear: AddRecursos().AddOutraCoisa()
    }
}
