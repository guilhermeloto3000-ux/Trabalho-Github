// componentes/ListaDeRecursos.jsx — AULA FINAL — NOVO (o código veio do App.jsx)
// Só MOSTRA a lista. Não chama a API: avisa a tela pelas funções onEditar, onRemover, onAnalisar.

// o texto que aparece ao lado do recurso (APLICADA 8)
function textoDaAnalise(analise) {
  if (!analise) return '';
  switch (analise.etapa) {
    case 'Enviando':    return 'enviando pedido...';
    case 'NaFila':      return 'na fila';
    case 'Processando': return 'processando...';
    case 'Concluida':   return `concluída: ${analise.recomendacao}`;
    default:            return `${analise.etapa}: ${analise.erro}`;   // Falhou, Recusada...
  }
}

function ListaDeRecursos({ recursos, analises, podeEditar, podeRemover, onEditar, onRemover, onAnalisar }) {
  return (
    <ul>
      {recursos.map(r => (
        <li key={r.id}>
          {r.nome} — capacidade: {r.capacidade}
          {podeEditar && <button onClick={() => onEditar(r)}>Editar</button>}
          {podeRemover && <button onClick={() => onRemover(r.id)}>Remover</button>}
          <button
            onClick={() => onAnalisar(r.id)}
            disabled={['Enviando', 'NaFila', 'Processando'].includes(analises[r.id]?.etapa)}
          >
            Analisar
          </button>
          <span> {textoDaAnalise(analises[r.id])}</span>
        </li>
      ))}
    </ul>
  );
}

export default ListaDeRecursos;
