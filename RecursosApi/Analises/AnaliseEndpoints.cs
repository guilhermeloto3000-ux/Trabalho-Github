// Analises/AnaliseEndpoints.cs
// APLICADA 6 — os endpoints da fila de análises.
// VERSÃO FINAL — MUDOU DE LUGAR: na Aula 6 eles estavam no Program.cs. Vieram para cá
// seguindo a mesma regra da Web 9 (RecursosEndpoints.cs): o Program.cs fica com a
// configuração, e cada grupo de endpoints mora no seu arquivo. O código é o mesmo.
//
// Estes endpoints continuam LIVRES (sem login), como na Aula 8 de Aplicada.
public static class AnaliseEndpoints
{
    public static void MapAnalises(this IEndpointRouteBuilder app)
    {
        // Política 1 — REJEITAR: se a fila está cheia, responde 503 na hora
        app.MapPost("/recursos/{id}/analise", (int id, FilaDeAnalises fila, StatusDasAnalises status) =>
        {
            var job = new AnalisarRecursoJob(id, DateTime.UtcNow);

            if (!fila.TentarEnfileirar(job))
            {
                status.RegistrarRejeitado();
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Fila de análises cheia",
                    detail: "Tente novamente em alguns segundos.");
            }

            status.RegistrarAceito();
            status.Atualizar(id, new AnaliseResultado(EtapaAnalise.NaFila));
            return Results.Accepted($"/recursos/{id}/analise", new { recursoId = id, etapa = "NaFila" });
        })
        .WithSummary("Pede a análise de um recurso (503 se a fila estiver cheia)");

        // Política 2 — ESPERAR: a requisição fica aguardando abrir espaço na fila.
        // O CancellationToken vem da própria requisição: se o cliente desistir, paramos de esperar.
        app.MapPost("/recursos/{id}/analise/espera",
            async (int id, FilaDeAnalises fila, StatusDasAnalises status, CancellationToken ct) =>
        {
            await fila.EnfileirarAsync(new AnalisarRecursoJob(id, DateTime.UtcNow), ct);

            status.RegistrarAceito();
            status.Atualizar(id, new AnaliseResultado(EtapaAnalise.NaFila));
            return Results.Accepted($"/recursos/{id}/analise", new { recursoId = id, etapa = "NaFila" });
        })
        .WithSummary("Pede a análise esperando vaga na fila");

        // Consulta do andamento de uma análise
        app.MapGet("/recursos/{id}/analise", (int id, StatusDasAnalises status) =>
            status.Obter(id) is { } resultado ? Results.Ok(resultado) : Results.NotFound())
        .WithSummary("Situação atual da análise de um recurso");

        // Painel da fila
        app.MapGet("/fila/status", (FilaDeAnalises fila, StatusDasAnalises status) =>
            Results.Ok(status.Resumo(fila.TamanhoAtual, fila.Capacidade)))
        .WithSummary("Painel da fila de análises");

        // DEMO — o que acontece quando várias threads fazem "++" na mesma variável
        app.MapGet("/demo/contador", () =>
        {
            int inseguro = 0;
            int seguro = 0;

            // Parallel.For roda o corpo em várias threads ao mesmo tempo
            Parallel.For(0, 1_000_000, _ =>
            {
                inseguro++;                          // ler, somar, gravar: 3 passos que podem se atropelar
                Interlocked.Increment(ref seguro);   // 1 passo só, atômico
            });

            return Results.Ok(new { esperado = 1_000_000, inseguro, seguro });
        });

        // APLICADA 8: o endereço onde os navegadores se conectam ao Hub.
        // AULA FINAL — MUDOU DE LUGAR: estava no Program.cs; é do mesmo assunto, então veio para cá.
        app.MapHub<AnaliseHub>("/hubs/analises");
    }
}
