// StatusDasAnalises.cs
// Guarda o estado de cada análise e as métricas da fila.
// Singleton: o worker ESCREVE aqui (em várias threads) e os endpoints LEEM (em outras threads).
// Por isso usamos ConcurrentDictionary: um dicionário que aguenta acesso simultâneo
// sem corromper os dados e sem precisarmos escrever lock.
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

// O atributo faz o JSON mostrar "Concluida" em vez do número 2.
[JsonConverter(typeof(JsonStringEnumConverter<EtapaAnalise>))]
public enum EtapaAnalise { NaFila, Processando, Concluida, Falhou }

public record AnaliseResultado(EtapaAnalise Etapa, string? Recomendacao = null, string? Erro = null);

public class StatusDasAnalises
{
    // Recurso Id -> situação da última análise pedida.
    private readonly ConcurrentDictionary<int, AnaliseResultado> _porRecurso = new();

    // Nome da métrica -> valor. Ex.: "aceitos" -> 12
    private readonly ConcurrentDictionary<string, long> _metricas = new();

    public void Atualizar(int recursoId, AnaliseResultado resultado) => _porRecurso[recursoId] = resultado;

    public AnaliseResultado? Obter(int recursoId)
        => _porRecurso.TryGetValue(recursoId, out var r) ? r : null;

    // AddOrUpdate é atômico: se duas threads somarem ao mesmo tempo, nenhuma soma se perde.
    // Um "_metricas[nome] = _metricas[nome] + 1" NÃO seria: são duas operações (ler e gravar).
    private long Somar(string nome, long valor)
        => _metricas.AddOrUpdate(nome, valor, (_, atual) => atual + valor);

    public void RegistrarAceito() => Somar("aceitos", 1);
    public void RegistrarRejeitado() => Somar("rejeitados", 1);
    public void RegistrarFalha() => Somar("falhas", 1);

    public void RegistrarProcessado(TimeSpan esperaNaFila)
    {
        Somar("processados", 1);
        Somar("esperaTotalMs", (long)esperaNaFila.TotalMilliseconds);
    }

    // Quantos consumidores estão usando o banco neste instante, e o maior valor já visto.
    public void EntrouNoBanco()
    {
        var agora = Somar("noBancoAgora", 1);
        _metricas.AddOrUpdate("picoNoBanco", agora, (_, pico) => Math.Max(pico, agora));
    }

    public void SaiuDoBanco() => Somar("noBancoAgora", -1);

    private long Ler(string nome) => _metricas.TryGetValue(nome, out var v) ? v : 0;

    public object Resumo(int tamanhoDaFila, int capacidade)
    {
        var processados = Ler("processados");
        return new
        {
            naFilaAgora = tamanhoDaFila,
            capacidade,
            aceitos = Ler("aceitos"),
            rejeitados = Ler("rejeitados"),
            processados,
            falhas = Ler("falhas"),
            esperaMediaMs = processados == 0 ? 0 : Ler("esperaTotalMs") / processados,
            noBancoAgora = Ler("noBancoAgora"),
            picoNoBanco = Ler("picoNoBanco")
        };
    }
}
