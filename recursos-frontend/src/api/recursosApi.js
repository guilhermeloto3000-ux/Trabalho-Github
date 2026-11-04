// api/recursosApi.js — AULA FINAL — NOVO
// TODAS as chamadas REST da API ficam neste arquivo, uma função por endpoint.
// As telas não escrevem fetch nem endereço: chamam estas funções.
// Cada função devolve a Response do fetch; quem chamou confere o status (ok, 401, 403...).
//
// Os endereços são RELATIVOS ('/recursos', e não 'http://localhost:5137/recursos'):
//   - em desenvolvimento, o Vite (5173) repassa para a API (5137) — veja vite.config.js;
//   - publicado, a página e a API estão no MESMO endereço.
// Antes, 'http://localhost:5137' aparecia em 9 lugares; num servidor, todos estariam errados.

// cabeçalhos do JSON e, se houver token, o Authorization (veio do App.jsx: cabecalhosComToken)
function cabecalhos(token) {
  const lista = { 'Content-Type': 'application/json' };
  if (token) {
    lista.Authorization = `Bearer ${token}`;
  }
  return lista;
}

// ----- Autenticação -----

// POST /auth/login -> 200 { token, nome, roles } | 401
export function entrar(usuario, senha) {
  return fetch('/auth/login', {
    method: 'POST',
    headers: cabecalhos(),
    body: JSON.stringify({ usuario, senha })
  });
}

// POST /auth/cadastro -> 201 | 400 (senha fraca) | 409 (usuário já existe)
export function cadastrar(usuario, senha) {
  return fetch('/auth/cadastro', {
    method: 'POST',
    headers: cabecalhos(),
    body: JSON.stringify({ usuario, senha })
  });
}

// ----- Recursos -----

// GET /recursos?pagina=1&tamanho=5&nomeContem=Sala -> 200 [ ... ] | 400
export function listarRecursos(pagina, tamanho, nomeContem) {
  const parametros = new URLSearchParams({ pagina, tamanho });
  if (nomeContem) {
    parametros.append('nomeContem', nomeContem);
  }
  return fetch(`/recursos?${parametros}`);
}

// POST /recursos (id null) ou PUT /recursos/{id} -> 201 | 204 | 400 | 401 | 404
export function salvarRecurso(id, corpo, token) {
  return fetch(id ? `/recursos/${id}` : '/recursos', {
    method: id ? 'PUT' : 'POST',
    headers: cabecalhos(token),
    body: JSON.stringify(corpo)
  });
}

// DELETE /recursos/{id} -> 204 | 401 | 403 | 404
export function removerRecurso(id, token) {
  return fetch(`/recursos/${id}`, {
    method: 'DELETE',
    headers: cabecalhos(token)
  });
}

// ----- Análises (livres, sem login) -----

// POST /recursos/{id}/analise -> 202 { recursoId, etapa } | 503 (fila cheia)
export function pedirAnalise(id) {
  return fetch(`/recursos/${id}/analise`, { method: 'POST' });
}

// GET /recursos/{id}/analise -> 200 { etapa, recomendacao, erro } | 404
export function obterAnalise(id) {
  return fetch(`/recursos/${id}/analise`);
}
