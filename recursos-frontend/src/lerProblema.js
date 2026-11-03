// lerProblema.js
// Transforma uma resposta de erro da API (Problem Details) em um texto para mostrar na tela.
export async function lerProblema(resposta) {
  const tipo = resposta.headers.get('content-type') ?? '';

  // Se a API não mandou Problem Details, não dá para ler o JSON com segurança.
  if (!tipo.includes('application/problem+json')) {
    return `Erro ${resposta.status}`;
  }

  const problema = await resposta.json();

  // 400 de validação: "errors" é um objeto { campo: [mensagens] }.
  if (problema.errors) {
    return Object.values(problema.errors).flat().join(' ');
  }

  // Outros erros: "detail" explica o caso; "title" é o resumo.
  return problema.detail ?? problema.title;
}