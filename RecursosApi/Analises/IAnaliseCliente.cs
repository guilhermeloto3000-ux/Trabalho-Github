// IAnaliseCliente.cs — APLICADA 8 — NOVO
// A lista do que o SERVIDOR pode chamar no NAVEGADOR.
// Cada método desta interface vira um evento que o JavaScript escuta com connection.on("...").
// Com a interface, o compilador confere o nome e os parâmetros: um erro de digitação
// em "AnaliseAtualizada" vira erro de compilação, e não um evento que nunca chega.
public interface IAnaliseCliente
{
    Task AnaliseAtualizada(AnaliseAtualizadaEvento evento);
}

// O que viaja no evento: só o necessário para a tela se atualizar.
// Nada de entidade do EF, nada de DbContext, nada de dados que o navegador não precisa.
public record AnaliseAtualizadaEvento(
    int RecursoId,
    EtapaAnalise Etapa,
    string? Recomendacao,
    string? Erro);
