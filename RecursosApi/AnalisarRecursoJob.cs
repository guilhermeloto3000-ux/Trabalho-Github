// AnalisarRecursoJob.cs
// O "pedido de trabalho" que viaja pela fila.
// É um record (imutável): depois de criado, ninguém altera — seguro para passar entre threads.
// Carrega só o mínimo: o Id do recurso e a hora em que entrou na fila.
// NUNCA coloque DbContext, Recurso rastreado pelo EF ou HttpContext aqui dentro:
// eles pertencem à requisição e morrem junto com ela.
public record AnalisarRecursoJob(int RecursoId, DateTime EnfileiradoEm);
