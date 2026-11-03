public class RecomendacaoService
{
    private readonly IRecomendadorRepository _repositorio;
    public RecomendacaoService(IRecomendadorRepository repositorio)
        => _repositorio = repositorio;

    public async Task<string?> ObterRecomendacaoAsync(int recursoId)
    {
        var recurso = await _repositorio.ObterPorIdAsync(recursoId);
        if (recurso is null) return null;

        var categoria = ClassificadorDeRecurso.Classificar(recurso);
        var recomendador = CatalogoDeRecomendadores.Buscar(categoria);
        return recomendador.Recomendar(recurso);
    }
}