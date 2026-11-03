using Microsoft.EntityFrameworkCore;

public class RecursoService
{
    private readonly RecursosApiContext _context;

    public RecursoService(RecursosApiContext context) => _context = context;

    // Aula 4 (Aplicada): AsNoTracking + projeção direta em DTO.
    // "nomeContem" é opcional — demonstra binding por query string (Web, Aula 3).
    // Continua igual: o RecursosApiController usa este método.
    public List<RecursoDto> Listar(string? nomeContem = null)
    {
        var consulta = _context.Recursos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nomeContem))
            consulta = consulta.Where(r => r.Nome.Contains(nomeContem));

        return consulta
            .Select(r => new RecursoDto(r.Id, r.Nome, r.Capacidade))
            .ToList();
    }

    // AULA 7 — NOVO: o mesmo Listar, mas devolvendo só uma página.
    public List<RecursoDto> ListarPagina(string? nomeContem, int pagina, int tamanho)
    {
        var consulta = _context.Recursos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nomeContem))
            consulta = consulta.Where(r => r.Nome.Contains(nomeContem));

        return consulta
            .OrderBy(r => r.Id)                 // ordem fixa: sem ela, as páginas podem mudar
            .Skip((pagina - 1) * tamanho)       // pula as páginas anteriores
            .Take(tamanho)                      // pega só esta página
            .Select(r => new RecursoDto(r.Id, r.Nome, r.Capacidade))
            .ToList();                          // SÓ AQUI vai ao banco
    }

    // Sem AsNoTracking aqui de propósito — usado também por Atualizar/Remover,
    // que precisam do rastreamento normal do EF Core pra funcionar.
    public Recurso? BuscarPorId(int id) => _context.Recursos.Find(id);

    public Recurso Criar(Recurso recurso)
    {
        _context.Recursos.Add(recurso);
        _context.SaveChanges();
        return recurso;
    }

    public bool Atualizar(int id, Recurso recursoAtualizado)
    {
        var recurso = BuscarPorId(id);
        if (recurso is null) return false;

        recurso.Nome = recursoAtualizado.Nome;
        recurso.Capacidade = recursoAtualizado.Capacidade;
        _context.SaveChanges();
        return true;
    }

    public bool Remover(int id)
    {
        var recurso = BuscarPorId(id);
        if (recurso is null) return false;

        _context.Recursos.Remove(recurso);
        _context.SaveChanges();
        return true;
    }
}