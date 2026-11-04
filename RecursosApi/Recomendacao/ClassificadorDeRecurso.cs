// Aula 1 (Aplicada): switch expression com padrões relacionais.
public static class ClassificadorDeRecurso
{
    public static CategoriaRecurso Classificar(Recurso? recurso) => recurso switch
    {
        null => CategoriaRecurso.Pequeno,
        { Capacidade: <= 10 } => CategoriaRecurso.Pequeno,
        { Capacidade: > 10 and <= 50 } => CategoriaRecurso.Medio,
        _ => CategoriaRecurso.Grande
    };
}
