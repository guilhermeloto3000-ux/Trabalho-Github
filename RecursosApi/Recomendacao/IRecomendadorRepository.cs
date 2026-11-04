public interface IRecomendadorRepository
{
    Task<Recurso?> ObterPorIdAsync(int id);
}
