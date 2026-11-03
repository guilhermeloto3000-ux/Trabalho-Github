// FilaDeAnalises.cs
// Embrulha um Channel<T>: uma fila feita para uma parte do código ESCREVER (produtor)
// e outra parte LER (consumidor), cada uma no seu ritmo, sem precisar de lock.
// Registrada como Singleton: a fila precisa ser a MESMA para o endpoint (quem escreve)
// e para o worker (quem lê). Se fosse Scoped, cada requisição teria sua própria fila vazia.
using System.Threading.Channels;

public class FilaDeAnalises
{
    private readonly Channel<AnalisarRecursoJob> _canal;

    public int Capacidade { get; }

    public FilaDeAnalises(int capacidade)
    {
        Capacidade = capacidade;

        // Bounded = fila com tamanho máximo. Unbounded = sem limite (cresce até acabar a memória).
        _canal = Channel.CreateBounded<AnalisarRecursoJob>(new BoundedChannelOptions(capacidade)
        {
            // O que fazer quando a fila está cheia e alguém chama WriteAsync:
            // Wait = o produtor espera abrir espaço (é o backpressure "empurrando" para trás).
            // DropNewest / DropOldest / DropWrite = descarta algum item sem avisar.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,   // teremos vários consumidores lendo
            SingleWriter = false    // várias requisições escrevendo ao mesmo tempo
        });
    }

    // Tenta colocar na fila SEM esperar. Devolve false se estiver cheia (ou encerrada).
    public bool TentarEnfileirar(AnalisarRecursoJob job) => _canal.Writer.TryWrite(job);

    // Coloca na fila ESPERANDO abrir espaço, se preciso. O token cancela a espera
    // (por exemplo, se o cliente desistir da requisição).
    public ValueTask EnfileirarAsync(AnalisarRecursoJob job, CancellationToken ct)
        => _canal.Writer.WriteAsync(job, ct);

    // Entrega os itens um a um, "dormindo" (sem gastar CPU) enquanto a fila está vazia.
    public IAsyncEnumerable<AnalisarRecursoJob> LerTodosAsync(CancellationToken ct)
        => _canal.Reader.ReadAllAsync(ct);

    // Quantos itens estão esperando agora (profundidade da fila).
    public int TamanhoAtual => _canal.Reader.Count;

    // Fecha a porta de entrada: ninguém mais consegue escrever.
    public void Encerrar() => _canal.Writer.TryComplete();
}
