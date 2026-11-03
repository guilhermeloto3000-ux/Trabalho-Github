// AnaliseHub.cs — APLICADA 8 — NOVO
// O Hub é a classe que recebe as conexões SignalR dos navegadores.
// Hub<IAnaliseCliente> = um Hub "tipado": os avisos para o navegador só podem usar
// os métodos de IAnaliseCliente.
// Um objeto do Hub é criado para CADA chamada e descartado logo depois (como um Scoped).
// Por isso ele pode receber o RecursoService (Scoped) no construtor, e NÃO pode guardar
// informação em campos entre uma chamada e outra.
using Microsoft.AspNetCore.SignalR;

public class AnaliseHub : Hub<IAnaliseCliente>
{
    private readonly RecursoService _recursos;
    private readonly ILogger<AnaliseHub> _logger;

    public AnaliseHub(RecursoService recursos, ILogger<AnaliseHub> logger)
    {
        _recursos = recursos;
        _logger = logger;
    }

    // Nome do grupo de um recurso. O Hub e o worker usam este MESMO método,
    // para os dois nunca escreverem o nome de jeitos diferentes.
    public static string Grupo(int recursoId) => $"recurso-{recursoId}";

    // O navegador chama: await conexao.invoke("AcompanharRecurso", 3)
    public async Task AcompanharRecurso(int recursoId)
    {
        // Verifica ANTES de colocar no grupo.
        // Hoje: o recurso existe? Com login (Web 8/9): este usuário pode ver este recurso?
        if (_recursos.BuscarPorId(recursoId) is null)
            throw new HubException($"Não existe recurso com id {recursoId}.");

        // Context.ConnectionId = identificador desta conexão (uma aba do navegador).
        await Groups.AddToGroupAsync(Context.ConnectionId, Grupo(recursoId));
        _logger.LogInformation("Conexão {Conexao} acompanha o recurso {Id}",
            Context.ConnectionId, recursoId);
    }

    // O navegador chama quando não quer mais receber avisos daquele recurso.
    public Task DeixarDeAcompanhar(int recursoId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, Grupo(recursoId));

    // Chamados pelo próprio SignalR quando uma conexão abre ou fecha. Só registramos no log.
    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Conexão aberta: {Conexao}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // Ao fechar, o SignalR tira a conexão de TODOS os grupos sozinho.
        _logger.LogInformation("Conexão fechada: {Conexao}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
