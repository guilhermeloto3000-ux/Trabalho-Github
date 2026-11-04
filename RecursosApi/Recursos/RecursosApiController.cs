using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/recursos-mvc")]
public class RecursosApiController : ControllerBase
{
    private readonly RecursoService _service;
    private readonly ILogger<RecursosApiController> _logger;

    public RecursosApiController(RecursoService service, ILogger<RecursosApiController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<List<RecursoDto>> ObterTodas([FromQuery] string? nomeContem)
    {
        return Ok(_service.Listar(nomeContem));
    }

    [HttpGet("{id}")]
    public ActionResult<RecursoDto> ObterPorId(int id)
    {
        var recurso = _service.BuscarPorId(id);
        if (recurso is null) return NotFound();

        return Ok(new RecursoDto(recurso.Id, recurso.Nome, recurso.Capacidade));
    }

    [HttpPost]
    public ActionResult<RecursoDto> Criar(CriarRecursoDto dto, [FromHeader(Name = "X-Client-Id")] string? clienteId)
    {
        _logger.LogInformation("Recurso criado pelo cliente {ClienteId}", clienteId ?? "desconhecido");

        var recurso = new Recurso { Nome = dto.Nome, Capacidade = dto.Capacidade };
        var criado = _service.Criar(recurso);
        var criadoDto = new RecursoDto(criado.Id, criado.Nome, criado.Capacidade);

        return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, criadoDto);
    }

    [HttpPut("{id}")]
    public IActionResult Atualizar(int id, AtualizarRecursoDto dto)
    {
        var recurso = new Recurso { Nome = dto.Nome, Capacidade = dto.Capacidade };
        var ok = _service.Atualizar(id, recurso);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public IActionResult Remover(int id)
    {
        var ok = _service.Remover(id);
        return ok ? NoContent() : NotFound();
    }
}
