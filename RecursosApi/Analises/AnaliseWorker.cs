// AnaliseWorker.cs
// O consumidor da fila. BackgroundService = uma tarefa que o ASP.NET inicia junto com a
// aplicação e que fica rodando "por trás", fora de qualquer requisição HTTP.
// Ele é criado UMA vez (Singleton, na prática), por isso NÃO pode receber o
// RecomendacaoService (Scoped) no construtor — cria um escopo novo para cada item.
using Microsoft.AspNetCore.SignalR;   // APLICADA 8 — NOVO

public class AnaliseWorker : BackgroundService
{
    // Quantos consumidores leem a fila ao mesmo tempo.
    private const int Consumidores = 4;

    // Quantos deles podem usar o banco ao mesmo tempo (as "vagas" do semáforo).
    // SemaphoreSlim(2) = contador com 2 vagas: a terceira thread espera até uma ser devolvida.
    private readonly SemaphoreSlim _vagasNoBanco = new(2);

    private readonly FilaDeAnalises _fila;
    private readonly StatusDasAnalises _status;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnaliseWorker> _logger;

    // APLICADA 8 — NOVO: o "controle remoto" do Hub, para avisar os navegadores de fora dele.
    private readonly IHubContext<AnaliseHub, IAnaliseCliente> _hub;

    public AnaliseWorker(FilaDeAnalises fila, StatusDasAnalises status,
        IServiceScopeFactory scopeFactory, ILogger<AnaliseWorker> logger,
        IHubContext<AnaliseHub, IAnaliseCliente> hub)                      // APLICADA 8 — MUDOU
    {
        _fila = fila;
        _status = status;
        _scopeFactory = scopeFactory;   // a "fábrica de escopos": Singleton, pode ser injetada aqui
        _logger = logger;
        _hub = hub;                     // Singleton: pode ser guardado no worker
    }

    // Chamado uma vez quando a aplicação sobe. stoppingToken é cancelado no Ctrl+C.
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Dispara N consumidores lendo a MESMA fila; cada item é entregue a apenas um deles.
        var consumidores = Enumerable.Range(1, Consumidores)
            .Select(numero => ConsumirAsync(numero, stoppingToken));

        return Task.WhenAll(consumidores);
    }

    private async Task ConsumirAsync(int numero, CancellationToken ct)
    {
        // await foreach "dorme" enquanto a fila está vazia e acorda quando chega item.
        await foreach (var job in _fila.LerTodosAsync(ct))
        {
            await ProcessarAsync(job, numero, ct);
        }
    }

    private async Task ProcessarAsync(AnalisarRecursoJob job, int consumidor, CancellationToken ct)
    {
        var espera = DateTime.UtcNow - job.EnfileiradoEm;
        await AtualizarEAvisarAsync(job.RecursoId,                          // APLICADA 8 — MUDOU
            new AnaliseResultado(EtapaAnalise.Processando));

        // try/catch POR ITEM: se um item falhar, os outros continuam.
        // Sem ele, a exceção escapa do ExecuteAsync e o .NET DERRUBA A API INTEIRA.
        try
        {
            // 1) Parte pesada que NÃO usa o banco (simulada). Fica FORA do semáforo:
            //    não faz sentido ocupar uma vaga do banco sem estar usando o banco.
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            string? texto;

            // 2) Parte que usa o banco: só entra quem conseguir uma vaga.
            await _vagasNoBanco.WaitAsync(ct);
            _status.EntrouNoBanco();
            try
            {
                // Escopo novo = DbContext novo, só deste item. Descartado no fim do using.
                await using var escopo = _scopeFactory.CreateAsyncScope();
                var service = escopo.ServiceProvider.GetRequiredService<RecomendacaoService>();

                texto = await service.ObterRecomendacaoAsync(job.RecursoId);

                // SQLite local responde rápido demais para vermos o limite; simulamos consulta lenta.
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
            finally
            {
                // finally SEMPRE roda, mesmo com exceção: a vaga nunca fica presa.
                _status.SaiuDoBanco();
                _vagasNoBanco.Release();
            }

            if (texto is null)
            {
                _status.RegistrarFalha();
                await AtualizarEAvisarAsync(job.RecursoId,                      // APLICADA 8 — MUDOU
                    new AnaliseResultado(EtapaAnalise.Falhou, Erro: "Recurso não encontrado"));
                return;
            }

            _status.RegistrarProcessado(espera);
            await AtualizarEAvisarAsync(job.RecursoId,                          // APLICADA 8 — MUDOU
                new AnaliseResultado(EtapaAnalise.Concluida, texto));
            _logger.LogInformation("Consumidor {C}: recurso {Id} -> {Texto} (esperou {Ms} ms na fila)",
                consumidor, job.RecursoId, texto, (int)espera.TotalMilliseconds);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Aplicação encerrando: não é falha do item, apenas deixamos o cancelamento seguir.
            throw;
        }
        catch (Exception ex)
        {
            _status.RegistrarFalha();
            await AtualizarEAvisarAsync(job.RecursoId,                          // APLICADA 8 — MUDOU
                new AnaliseResultado(EtapaAnalise.Falhou, Erro: ex.Message));
            _logger.LogError(ex, "Consumidor {C}: falha ao analisar o recurso {Id}", consumidor, job.RecursoId);
        }
    }

    // APLICADA 8 — NOVO: grava a situação e DEPOIS avisa quem acompanha aquele recurso.
    private async Task AtualizarEAvisarAsync(int recursoId, AnaliseResultado resultado)
    {
        // 1) Grava primeiro. Se o navegador perder o aviso, o GET /recursos/{id}/analise
        //    já devolve a situação nova.
        _status.Atualizar(recursoId, resultado);

        // 2) Avisa só o grupo daquele recurso, com os dados mínimos.
        try
        {
            var evento = new AnaliseAtualizadaEvento(
                recursoId, resultado.Etapa, resultado.Recomendacao, resultado.Erro);

            await _hub.Clients.Group(AnaliseHub.Grupo(recursoId))
                      .AnaliseAtualizada(evento);
        }
        catch (Exception ex)
        {
            // O aviso falhou, mas a situação JÁ está gravada: não marcamos a análise como falha.
            _logger.LogWarning(ex, "Não foi possível avisar o grupo do recurso {Id}", recursoId);
        }
    }

    // Chamado no Ctrl+C: fecha a entrada da fila e avisa o que ficou para trás.
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _fila.Encerrar();
        _logger.LogWarning("Encerrando: {Pendentes} análise(s) na fila serão descartadas.", _fila.TamanhoAtual);
        await base.StopAsync(cancellationToken);
    }
}
