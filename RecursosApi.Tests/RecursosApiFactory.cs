// RecursosApi.Tests/RecursosApiFactory.cs
using System.Net.Http.Headers;                    // VERSÃO FINAL — NOVO: AuthenticationHeaderValue
using Microsoft.AspNetCore.Hosting;               // IWebHostBuilder
using Microsoft.AspNetCore.Identity;              // VERSÃO FINAL — NOVO: IdentityUser
using Microsoft.AspNetCore.Mvc.Testing;           // WebApplicationFactory
using Microsoft.Extensions.DependencyInjection;   // CreateScope, GetRequiredService
using Microsoft.Extensions.Hosting;               // IHost, IHostBuilder

public class RecursosApiFactory : WebApplicationFactory<Program>
{
    // Roda ANTES de a API subir: troca a connection string.
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:RecursosDb", "Data Source=recursos-testes.db");

        // WEB 9 — NOVO: ambiente próprio dos testes (o seed de usuários só roda em Development).
        builder.UseEnvironment("Testing");

        // WEB 9 — NOVO: chave só para testes (o User Secrets só é lido em Development).
        builder.UseSetting("Jwt:Chave", "chave-usada-somente-nos-testes-automatizados");
    }

    // Roda quando a API é montada: apaga e recria o banco de testes.
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var escopo = host.Services.CreateScope();
        var contexto = escopo.ServiceProvider.GetRequiredService<RecursosApiContext>();

        contexto.Database.EnsureDeleted();
        contexto.Database.EnsureCreated();

        // WEB 9 — NOVO: o seed só roda em Development; nos testes criamos as roles aqui,
        // porque o cadastro (POST /auth/cadastro) coloca o usuário novo na role "Usuario".
        var roles = escopo.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var nome in new[] { "Usuario", "Administrador" })
            roles.CreateAsync(new IdentityRole(nome)).GetAwaiter().GetResult();

        return host;
    }

    // VERSÃO FINAL — NOVO: um HttpClient que já envia um token válido no cabeçalho
    // Authorization. O token é gerado pelo MESMO TokenService da API, com a chave de teste,
    // para um usuário que não precisa existir no banco: a API só confere a assinatura e as claims.
    public HttpClient CriarClienteComLogin(string role = "Usuario")
    {
        var cliente = CreateClient();

        using var escopo = Services.CreateScope();
        var tokens = escopo.ServiceProvider.GetRequiredService<TokenService>();
        var token = tokens.GerarToken(new IdentityUser("teste"), new List<string> { role });

        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}
