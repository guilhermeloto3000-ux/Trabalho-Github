[Recomendador(CategoriaRecurso.Pequeno)]
public class RecomendadorPequeno : IRecomendador
{
    public string Recomendar(Recurso recurso) =>
        $"{recurso.Nome} é ideal para uso individual ou pequenos grupos.";
}

[Recomendador(CategoriaRecurso.Medio)]
public class RecomendadorMedio : IRecomendador
{
    public string Recomendar(Recurso recurso) =>
        $"{recurso.Nome} comporta bem reuniões de porte médio.";
}

[Recomendador(CategoriaRecurso.Grande)]
public class RecomendadorGrande : IRecomendador
{
    public string Recomendar(Recurso recurso) =>
        $"{recurso.Nome} é indicado para eventos ou turmas grandes.";
}
