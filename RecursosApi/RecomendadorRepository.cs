using Microsoft.EntityFrameworkCore;
public class RecomendadorRepository : IRecomendadorRepository
{
    private readonly RecursosApiContext _context;
    public RecomendadorRepository(RecursosApiContext context)
        => _context = context;

    public Task<Recurso?> ObterPorIdAsync(int id) =>
        _context.Recursos.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
}
