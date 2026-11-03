using Microsoft.AspNetCore.Mvc;

// WEB 9 — NOVO: os endpoints de Recurso saíram do Program.cs e vieram para cá.
// O código de cada endpoint é o MESMO da Aula 7; o que muda:
//   1) as rotas ficam relativas ao grupo "/recursos";
//   2) POST, PUT e DELETE ganharam .RequireAuthorization(...).
public static class RecursosEndpoints
{
    // "this IEndpointRouteBuilder app": método de extensão — permite escrever app.MapRecursos() no Program.cs
    public static void MapRecursos(this IEndpointRouteBuilder app)
    {
        // MapGroup: todos os endpoints abaixo começam com "/recursos"
        var grupo = app.MapGroup("/recursos");

        // GET /recursos — LIVRE (sem login)
        grupo.MapGet("", (RecursoService s, string? nomeContem, int pagina = 1, int tamanho = 10) =>
        {
            if (pagina < 1 || tamanho < 1 || tamanho > 50)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["paginacao"] = ["Use pagina >= 1 e tamanho entre 1 e 50."]
                });

            return Results.Ok(s.ListarPagina(nomeContem, pagina, tamanho));
        })
        .WithSummary("Lista recursos, com filtro por nome e paginação")
        .Produces<List<RecursoDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem();

        // GET /recursos/{id} — LIVRE
        grupo.MapGet("/{id}", (int id, RecursoService s) =>
        {
            var r = s.BuscarPorId(id);
            return r is not null
                ? Results.Ok(new RecursoDto(r.Id, r.Nome, r.Capacidade))
                : NaoEncontrado(id);
        })
        .WithSummary("Busca um recurso pelo id")
        .Produces<RecursoDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // POST /recursos — PRECISA ESTAR LOGADO
        grupo.MapPost("", (
            CriarRecursoDto dto,
            RecursoService s,
            [FromHeader(Name = "X-Client-Id")] string? clienteId,
            ILogger<Program> logger) =>
        {
            var erros = ValidadorDeRecurso.Validar(dto.Nome, dto.Capacidade);
            if (erros.Count > 0)
                return Results.ValidationProblem(erros, title: "Dados do recurso inválidos");

            logger.LogInformation("Recurso criado pelo cliente {ClienteId}", clienteId ?? "desconhecido");

            var recurso = new Recurso { Nome = dto.Nome, Capacidade = dto.Capacidade };
            var criado = s.Criar(recurso);
            var criadoDto = new RecursoDto(criado.Id, criado.Nome, criado.Capacidade);
            return Results.Created($"/recursos/{criado.Id}", criadoDto);
        })
        .RequireAuthorization()                                   // WEB 9 — NOVO
        .WithSummary("Cria um recurso")
        .Produces<RecursoDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        // PUT /recursos/{id} — PRECISA ESTAR LOGADO
        grupo.MapPut("/{id}", (int id, AtualizarRecursoDto dto, RecursoService s) =>
        {
            var erros = ValidadorDeRecurso.Validar(dto.Nome, dto.Capacidade);
            if (erros.Count > 0)
                return Results.ValidationProblem(erros, title: "Dados do recurso inválidos");

            var recurso = new Recurso { Nome = dto.Nome, Capacidade = dto.Capacidade };
            var ok = s.Atualizar(id, recurso);
            return ok ? Results.NoContent() : NaoEncontrado(id);
        })
        .RequireAuthorization()                                   // WEB 9 — NOVO
        .WithSummary("Substitui os dados de um recurso")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status404NotFound);

        // DELETE /recursos/{id} — SÓ ADMINISTRADOR
        grupo.MapDelete("/{id}", (int id, RecursoService s) =>
        {
            var ok = s.Remover(id);
            return ok ? Results.NoContent() : NaoEncontrado(id);
        })
        .RequireAuthorization("SomenteAdministrador")             // WEB 9 — NOVO
        .WithSummary("Remove um recurso")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // GET /recursos/{id}/recomendacao — LIVRE (Aula 5 de Aplicada)
        grupo.MapGet("/{id}/recomendacao", async (int id, RecomendacaoService service) =>
        {
            var texto = await service.ObterRecomendacaoAsync(id);
            return texto is null
                ? Results.NotFound()
                : Results.Ok(new RecomendacaoResponse(texto));
        });
    }

    // Veio do Program.cs: o mesmo 404 para GET, PUT e DELETE.
    private static IResult NaoEncontrado(int id) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Recurso não encontrado",
            detail: $"Não existe recurso com id {id}.");
}
