// ValidadorDeRecurso.cs
// Regras de entrada de um recurso, usadas no POST e no PUT.
// Devolve os erros no formato que Results.ValidationProblem espera:
// nome do campo -> lista de mensagens daquele campo.
public static class ValidadorDeRecurso
{
    public const int NomeMaximo = 100;
    public const int CapacidadeMaxima = 1000;

    public static Dictionary<string, string[]> Validar(string? nome, int capacidade)
    {
        var erros = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(nome))
            erros["nome"] = ["O nome é obrigatório."];
        else if (nome.Length > NomeMaximo)
            erros["nome"] = [$"O nome pode ter no máximo {NomeMaximo} caracteres."];

        if (capacidade < 1 || capacidade > CapacidadeMaxima)
            erros["capacidade"] = [$"A capacidade deve estar entre 1 e {CapacidadeMaxima}."];

        return erros;
    }
}