// RecursosApi.Tests/RecursosEndpointsTests.cs   (arquivo NOVO)
using System.Net;                 // HttpStatusCode
using System.Net.Http.Json;       // PostAsJsonAsync, ReadFromJsonAsync, GetFromJsonAsync
using System.Text;                // Encoding
using Microsoft.AspNetCore.Http;  // HttpValidationProblemDetails
using Microsoft.AspNetCore.Mvc;   // ProblemDetails

// IClassFixture: o xUnit cria UMA fábrica para a classe toda e a entrega no
// construtor. A API sobe uma vez, não uma vez por teste.
// VERSÃO FINAL — NOVO: as duas classes de integração (esta e AuthEndpointsTests) usam o
// MESMO recursos-testes.db. Na mesma [Collection], o xUnit roda uma classe depois da outra,
// e uma não apaga o banco no meio dos testes da outra.
[Collection("BancoDeTestes")]
public class RecursosEndpointsTests : IClassFixture<RecursosApiFactory>
{
    private readonly HttpClient _cliente;
    private readonly HttpClient _clienteLogado;   // VERSÃO FINAL — NOVO: envia token de "Usuario"
    private readonly HttpClient _clienteAdmin;    // VERSÃO FINAL — NOVO: envia token de "Administrador"

    // o xUnit cria um objeto desta classe para CADA teste, chamando este construtor
    public RecursosEndpointsTests(RecursosApiFactory fabrica)
    {
        _cliente = fabrica.CreateClient();   // HttpClient já apontado para a API (sem token)

        // VERSÃO FINAL — NOVO: desde a Web 9, POST e PUT exigem login e DELETE exige Administrador.
        _clienteLogado = fabrica.CriarClienteComLogin("Usuario");
        _clienteAdmin = fabrica.CriarClienteComLogin("Administrador");
    }

        [Fact]
    public async Task GetLista_Responde200()   // teste assíncrono: devolve Task
    {
        // await: espera a resposta sem travar a thread (Aula 6)
        var resposta = await _cliente.GetAsync("/recursos");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);   // OK = 200
    }

    [Fact]
    public async Task Post_DadosValidos_Responde201ComLocation()
    {
        var novo = new CriarRecursoDto("Laboratório 1", 30);   // o DTO que a API recebe
        // PostAsJsonAsync: converte o objeto em JSON e envia no corpo do POST
        var resposta = await _clienteLogado.PostAsJsonAsync("/recursos", novo);   // MUDOU: com token

        Assert.Equal(HttpStatusCode.Created, resposta.StatusCode);                        // (1)
        var criado = await resposta.Content.ReadFromJsonAsync<RecursoDto>();             // (2)
        Assert.NotNull(criado);
        Assert.Equal("Laboratório 1", criado.Nome);                                      // (3)
        Assert.Equal($"/recursos/{criado.Id}", resposta.Headers.Location?.ToString());   // (4)
    }

    [Fact]
    public async Task Post_DepoisGet_DevolveOMesmoRecurso()
    {
        var post = await _clienteLogado.PostAsJsonAsync("/recursos",   // MUDOU: com token
            new CriarRecursoDto("Auditório", 200));
        var criado = await post.Content.ReadFromJsonAsync<RecursoDto>();

        // GetFromJsonAsync: faz o GET e já converte. "!": não é null aqui
        var lido = await _cliente.GetFromJsonAsync<RecursoDto>($"/recursos/{criado!.Id}");

        Assert.Equal(criado, lido);   // record compara pelo conteúdo
    }

    [Fact]
    public async Task Post_DadosInvalidos_Responde400ComErros()
    {
        var invalido = new CriarRecursoDto("", 0);   // nome vazio e capacidade 0

        var resposta = await _clienteLogado.PostAsJsonAsync("/recursos", invalido);   // MUDOU: com token

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        // classe do ASP.NET para o JSON de Results.ValidationProblem
        var problema = await resposta.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.Contains("nome", problema!.Errors);
        Assert.Contains("capacidade", problema.Errors);
    }

    [Fact]
    public async Task GetPorId_Inexistente_Responde404ComProblemDetails()
    {
        var resposta = await _cliente.GetAsync("/recursos/999999");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);
        Assert.Equal("application/problem+json",
            resposta.Content.Headers.ContentType?.MediaType);
        var problema = await resposta.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Recurso não encontrado", problema!.Title);   // do NaoEncontrado(id)
    }


// corpo devolvido:  { "title": "Dados do recurso inválidos", "status": 400,
//   "errors": { "nome": ["O nome é obrigatório."], "capacidade": ["..."] } }
// problema.Errors é o objeto "errors" desse JSON, como dicionário.


    // REGRESSÃO: o bug real da Aula 7 de Web, que respondia 500.
    [Fact]
    public async Task Post_CapacidadeComTexto_Responde400()
    {
        // """ ... """: texto bruto; as aspas do JSON não precisam de \"
        var json = """{ "nome": "Sala", "capacidade": "abc" }""";
        var corpo = new StringContent(json, Encoding.UTF8, "application/json");

        // PostAsync envia o texto como está: um CriarRecursoDto não
        // consegue ter "abc" numa propriedade int.
        var resposta = await _clienteLogado.PostAsync("/recursos", corpo);   // MUDOU: com token

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
    // ---------------------------------------------------------
    // VERSÃO FINAL — NOVO: a matriz 401 / 403 da Web 9, conferida por teste.
    // ---------------------------------------------------------

    [Fact]
    public async Task Post_SemToken_Responde401()
    {
        var resposta = await _cliente.PostAsJsonAsync("/recursos", new CriarRecursoDto("Sala", 10));

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);   // 401: não sei quem é
    }

    [Fact]
    public async Task Delete_ComUsuarioComum_Responde403()
    {
        var post = await _clienteLogado.PostAsJsonAsync("/recursos", new CriarRecursoDto("Sala", 10));
        var criado = await post.Content.ReadFromJsonAsync<RecursoDto>();

        var resposta = await _clienteLogado.DeleteAsync($"/recursos/{criado!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, resposta.StatusCode);      // 403: sei quem é, mas não pode
    }

    [Fact]
    public async Task Delete_ComAdministrador_Responde204()
    {
        var post = await _clienteLogado.PostAsJsonAsync("/recursos", new CriarRecursoDto("Sala", 10));
        var criado = await post.Content.ReadFromJsonAsync<RecursoDto>();

        var resposta = await _clienteAdmin.DeleteAsync($"/recursos/{criado!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, resposta.StatusCode);      // 204: removido
    }

    // AULA FINAL — NOVO: o endereço que o Render chama depois de cada deploy.
    [Fact]
    public async Task Health_Responde200()
    {
        var resposta = await _cliente.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Equal("Healthy", await resposta.Content.ReadAsStringAsync());
    }
}   // fim da classe
