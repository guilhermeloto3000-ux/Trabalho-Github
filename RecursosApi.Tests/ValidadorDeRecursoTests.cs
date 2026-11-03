public class ValidadorDeRecursoTests
{
    [Fact]
    public void Validar_DadosValidos_NaoTemErros()
    {
        // 1) Preparar
        var nome = "Laboratório 1";
        var capacidade = 30;

        // 2) Executar: ValidadorDeRecurso é static, chamamos pela classe, sem new
        var erros = ValidadorDeRecurso.Validar(nome, capacidade);

        // 3) Conferir: dicionário de erros vazio = dados aceitos
        Assert.Empty(erros);
    }

    [Theory]
    [InlineData("", 30, "nome")]             // nome vazio
    [InlineData("Sala", 0, "capacidade")]    // abaixo do mínimo
    [InlineData("Sala", 1001, "capacidade")] // acima do máximo
    // Os valores de cada InlineData entram nos parâmetros, na mesma ordem.
    public void Validar_DadoInvalido_TemErroNoCampo(
        string nome, int capacidade, string campo)
    {
        var erros = ValidadorDeRecurso.Validar(nome, capacidade);

        // o dicionário tem a chave do campo errado ("nome" ou "capacidade")
        Assert.Contains(campo, erros);
    }

    [Theory]
    [InlineData(1)]      // menor valor permitido
    [InlineData(1000)]   // maior valor permitido (CapacidadeMaxima)
    public void Validar_CapacidadeNoLimite_EhValida(int capacidade)
    {
        var erros = ValidadorDeRecurso.Validar("Sala", capacidade);

        Assert.Empty(erros);
    }
}

