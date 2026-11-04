using System.Reflection;

public static class CatalogoDeRecomendadores
{
    private static readonly Dictionary<CategoriaRecurso, Type> _catalogo = Construir();

    private static Dictionary<CategoriaRecurso, Type> Construir()
    {
        var catalogo = new Dictionary<CategoriaRecurso, Type>();

        var tipos = typeof(CatalogoDeRecomendadores).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => typeof(IRecomendador).IsAssignableFrom(t));

        foreach (var tipo in tipos)
        {
            var atributo = tipo.GetCustomAttribute<RecomendadorAttribute>();
            if (atributo is null)
                throw new InvalidOperationException($"{tipo.Name} não tem o atributo [Recomendador].");

            if (catalogo.ContainsKey(atributo.Categoria))
                throw new InvalidOperationException($"Categoria {atributo.Categoria} já registrada por {catalogo[atributo.Categoria].Name}.");

            if (tipo.GetConstructor(Type.EmptyTypes) is null)
                throw new InvalidOperationException($"{tipo.Name} precisa de um construtor público sem parâmetros.");

            catalogo[atributo.Categoria] = tipo;
        }

        return catalogo;
    }

    public static IRecomendador Buscar(CategoriaRecurso categoria)
    {
        if (!_catalogo.TryGetValue(categoria, out var tipo))
            throw new InvalidOperationException($"Nenhum recomendador registrado para a categoria {categoria}.");

        return (IRecomendador)Activator.CreateInstance(tipo)!;
    }
}
