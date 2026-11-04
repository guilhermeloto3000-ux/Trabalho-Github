[AttributeUsage(AttributeTargets.Class)]
public class RecomendadorAttribute : Attribute
{
    public CategoriaRecurso Categoria { get; }

    public RecomendadorAttribute(CategoriaRecurso categoria) => Categoria = categoria;
}
