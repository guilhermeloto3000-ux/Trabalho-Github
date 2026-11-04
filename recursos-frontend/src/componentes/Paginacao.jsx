// componentes/Paginacao.jsx — AULA FINAL — NOVO (o código veio do App.jsx, AULA 7)
// Veio MENOS que o tamanho da página? Então é a última: "Próxima" desliga (temProxima = false).
function Paginacao({ pagina, temProxima, carregando, onMudar }) {
  return (
    <div>
      <button onClick={() => onMudar(pagina - 1)} disabled={pagina <= 1 || carregando}>
        Anterior
      </button>
      <span> Página {pagina} </span>
      <button onClick={() => onMudar(pagina + 1)} disabled={!temProxima || carregando}>
        Próxima
      </button>
    </div>
  );
}

export default Paginacao;
