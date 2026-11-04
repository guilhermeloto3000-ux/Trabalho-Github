// RecursosApi.Tests/AuthEndpointsTests.cs   (WEB 9 — NOVO: testes do cadastro)
using System.Net;                 // HttpStatusCode
using System.Net.Http.Json;       // PostAsJsonAsync, ReadFromJsonAsync

[Collection("BancoDeTestes")]   // mesma coleção da RecursosEndpointsTests: não roda em paralelo com ela
public class AuthEndpointsTests : IClassFixture<RecursosApiFactory>
{
    private readonly HttpClient _cliente;

    public AuthEndpointsTests(RecursosApiFactory fabrica)
    {
        _cliente = fabrica.CreateClient();   // sem token: o cadastro e o login são livres
    }

    [Fact]
    public async Task Cadastro_Novo_Responde201_EDepoisEntra()
    {
        var cadastro = await _cliente.PostAsJsonAsync("/auth/cadastro",
            new CadastroDto("bia", "Senha.Bia123"));
        Assert.Equal(HttpStatusCode.Created, cadastro.StatusCode);

        // a mesma senha agora funciona no login, e o token vem com a role Usuario
        var login = await _cliente.PostAsJsonAsync("/auth/login", new LoginDto("bia", "Senha.Bia123"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var resposta = await login.Content.ReadFromJsonAsync<LoginRespostaDto>();
        Assert.Contains("Usuario", resposta!.Roles);
    }

    [Fact]
    public async Task Cadastro_NomeRepetido_Responde409()
    {
        await _cliente.PostAsJsonAsync("/auth/cadastro", new CadastroDto("caio", "Senha.Caio123"));

        var deNovo = await _cliente.PostAsJsonAsync("/auth/cadastro", new CadastroDto("caio", "Outra.Senha1"));

        Assert.Equal(HttpStatusCode.Conflict, deNovo.StatusCode);   // 409
    }

    [Fact]
    public async Task Cadastro_SenhaFraca_Responde400()
    {
        var resposta = await _cliente.PostAsJsonAsync("/auth/cadastro", new CadastroDto("davi", "123"));

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}
