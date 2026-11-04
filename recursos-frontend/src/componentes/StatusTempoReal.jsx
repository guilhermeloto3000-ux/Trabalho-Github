// componentes/StatusTempoReal.jsx — AULA FINAL — NOVO (o código veio do App.jsx, APLICADA 8)
function StatusTempoReal({ tempoReal, onReconectar }) {
  return (
    <p>
      Tempo real: {tempoReal}
      {tempoReal === 'desconectado' && <button onClick={onReconectar}>Reconectar</button>}
    </p>
  );
}

export default StatusTempoReal;
