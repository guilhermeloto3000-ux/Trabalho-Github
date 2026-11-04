// telas/Recursos.jsx — AULA FINAL — NOVO (a manutenção de recursos que estava no App.jsx)
// A tela guarda o estado (lista, página, filtro, recurso em edição) e chama a API.
// O desenho de cada parte ficou nos componentes da pasta componentes/.
import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router';
import { listarRecursos, removerRecurso } from '../api/recursosApi.js';
import { lerProblema } from '../api/lerProblema.js';
import { useSessao } from '../sessao/useSessao.js';
import { useAnalises } from '../tempoReal/useAnalises.js';
import ListaDeRecursos from '../componentes/ListaDeRecursos.jsx';
import FormularioRecurso from '../componentes/FormularioRecurso.jsx';
import Paginacao from '../componentes/Paginacao.jsx';
import StatusTempoReal from '../componentes/StatusTempoReal.jsx';

// quantos recursos aparecem por página (AULA 7)
const TAMANHO = 5;

function Recursos() {
  const { sessao, encerrarSessao, ehAdministrador } = useSessao();
  const { analises, tempoReal, analisarRecurso, reconectar } = useAnalises();
  const navegar = useNavigate();

  const [recursos, setRecursos] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState(null);
  const [pagina, setPagina] = useState(1);
  const [filtroNome, setFiltroNome] = useState('');
  // o recurso sendo editado — null significa "estamos criando"
  const [recursoEditando, setRecursoEditando] = useState(null);

  useEffect(() => {
    buscarRecursos(1);   // a tela abre na página 1
  }, []);

  async function buscarRecursos(paginaDesejada = pagina) {
    setCarregando(true);
    setErro(null);
    try {
      const resposta = await listarRecursos(paginaDesejada, TAMANHO, filtroNome);   // GET /recursos
      if (!resposta.ok) {
        setErro(await lerProblema(resposta));
        return;
      }
      setRecursos(await resposta.json());
      setPagina(paginaDesejada);
    } catch {
      setErro('Não foi possível conectar à API.');
    } finally {
      setCarregando(false);
    }
  }

  function aplicarFiltro(evento) {
    evento.preventDefault();
    buscarRecursos(1);   // filtro novo sempre volta para a página 1
  }

  // a API respondeu 401 -> token vencido ou inválido -> tela de login, com um aviso
  function sessaoExpirou() {
    encerrarSessao();
    navegar('/login', { state: { aviso: 'Sua sessão expirou. Entre de novo.' } });
  }

  async function remover(id) {
    const resposta = await removerRecurso(id, sessao?.token);   // DELETE /recursos/{id}

    if (resposta.status === 401) { sessaoExpirou(); return; }
    if (resposta.status === 403) { setErro('Só administradores podem remover recursos.'); return; }
    if (!resposta.ok) { setErro(await lerProblema(resposta)); return; }

    if (recursoEditando?.id === id) setRecursoEditando(null);   // não deixa o formulário "preso"
    buscarRecursos();
  }

  function salvo() {
    setRecursoEditando(null);   // volta ao modo "criar"
    buscarRecursos();           // recarrega a página ATUAL
  }

  return (
    <div>
      <StatusTempoReal tempoReal={tempoReal} onReconectar={reconectar} />

      <form onSubmit={aplicarFiltro}>
        <input placeholder="Filtrar por nome" value={filtroNome} onChange={e => setFiltroNome(e.target.value)} />
        <button type="submit">Filtrar</button>
      </form>

      {carregando && <p>Carregando...</p>}
      {erro && <p>Erro: {erro}</p>}
      {!carregando && !erro && recursos.length === 0 && <p>Nenhum recurso nesta página.</p>}

      <ListaDeRecursos
        recursos={recursos}
        analises={analises}
        podeEditar={sessao !== null}
        podeRemover={ehAdministrador}
        onEditar={setRecursoEditando}
        onRemover={remover}
        onAnalisar={analisarRecurso}
      />

      <Paginacao
        pagina={pagina}
        temProxima={recursos.length === TAMANHO}
        carregando={carregando}
        onMudar={buscarRecursos}
      />

      {/* o formulário só aparece para quem entrou */}
      {sessao && (
        <FormularioRecurso
          key={recursoEditando?.id ?? 'novo'}
          recurso={recursoEditando}
          token={sessao.token}
          onSalvo={salvo}
          onCancelar={() => setRecursoEditando(null)}
          onSessaoExpirou={sessaoExpirou}
        />
      )}
    </div>
  );
}

export default Recursos;
