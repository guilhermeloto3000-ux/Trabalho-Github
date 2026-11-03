import { useState, useEffect, useRef } from 'react';   // APLICADA 8 — MUDOU: + useRef
import './App.css';
import { lerProblema } from './lerProblema';   // AULA 7 — NOVO
import { criarConexaoAnalises, EstadoDaConexao } from './conexaoAnalises';   // APLICADA 8 — NOVO

// AULA 7 — NOVO: quantos recursos aparecem por página.
const TAMANHO = 5;

function App() {
  const [recursos, setRecursos] = useState([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState(null);

  const [nome, setNome] = useState('');
  const [capacidade, setCapacidade] = useState('');
  const [erroFormulario, setErroFormulario] = useState(null);

  // guarda o id do recurso sendo editado — null significa "estamos criando", não editando
  const [recursoEditando, setRecursoEditando] = useState(null);

  // AULA 7 — NOVO: página que está na tela e texto do filtro
  const [pagina, setPagina] = useState(1);
  const [filtroNome, setFiltroNome] = useState('');

  // WEB 9 — NOVO: sessão = o que o login devolveu ({ token, nome, roles }); null = ninguém logado.
  // Fica só na memória: some ao recarregar a página (F5).
  const [sessao, setSessao] = useState(null);
  const [loginUsuario, setLoginUsuario] = useState('');
  const [loginSenha, setLoginSenha] = useState('');
  const [erroLogin, setErroLogin] = useState(null);

  // WEB 9 — NOVO: só serve para esconder o botão Remover; quem decide de verdade é a API.
  const ehAdministrador = sessao !== null && sessao.roles.includes('Administrador');

  // APLICADA 8 — NOVO: situação da análise de cada recurso. Ex.: { 3: { etapa: 'Processando' } }
  const [analises, setAnalises] = useState({});
  // APLICADA 8 — NOVO: estado da conexão em tempo real, para mostrar na tela
  const [tempoReal, setTempoReal] = useState('conectando');

  // APLICADA 8 — NOVO: useRef guarda um valor que sobrevive às renderizações SEM redesenhar a tela.
  const conexaoRef = useRef(null);            // a conexão SignalR
  const acompanhados = useRef(new Set());     // ids dos recursos que esta aba acompanha

  useEffect(() => {
    buscarRecursos(1);   // AULA 7 — MUDOU: a tela abre na página 1
  }, []);

  // APLICADA 8 — NOVO: abre a conexão com o Hub quando a tela aparece e fecha quando ela some.
  useEffect(() => {
    const conexao = criarConexaoAnalises();
    conexaoRef.current = conexao;
    let ativa = true;   // false depois da limpeza: callbacks atrasados não mexem mais na tela

    // (1) o evento que o servidor dispara: o mesmo nome do método de IAnaliseCliente
    conexao.on('AnaliseAtualizada', evento => {
      atualizarAnalise(evento.recursoId, evento);
    });

    // (2) callbacks de estado da reconexão automática
    conexao.onreconnecting(() => ativa && setTempoReal('reconectando'));
    conexao.onreconnected(async () => {
      if (!ativa) return;
      setTempoReal('conectado');
      await reconciliar(conexao);   // pode ter perdido avisos enquanto estava fora
    });
    conexao.onclose(() => ativa && setTempoReal('desconectado'));

    // (3) conecta
    conexao.start()
      .then(() => ativa && setTempoReal('conectado'))
      .catch(() => ativa && setTempoReal('desconectado'));

    // (4) limpeza: a tela saiu (ou o StrictMode montou duas vezes em desenvolvimento)
    return () => {
      ativa = false;
      conexao.stop();
    };
  }, []);


  // AULA 7 — MUDOU: async/await, paginação, filtro e leitura de Problem Details.
  // paginaDesejada = pagina : se ninguém passar um número, usa a página atual.
  async function buscarRecursos(paginaDesejada = pagina) {
    setCarregando(true);
    setErro(null);

    // URLSearchParams monta "pagina=2&tamanho=5&nomeContem=Sala"
    // e codifica espaços e acentos.
    const parametros = new URLSearchParams({ pagina: paginaDesejada, tamanho: TAMANHO });
    if (filtroNome) {
      parametros.append('nomeContem', filtroNome);
    }

    try {
      const resposta = await fetch(`http://localhost:5137/recursos?${parametros}`);

      if (!resposta.ok) {                         // a API respondeu COM erro
        setErro(await lerProblema(resposta));
        return;
      }

      setRecursos(await resposta.json());         // continua sendo um array
      setPagina(paginaDesejada);
    } catch {
      setErro('Não foi possível conectar à API.'); // NÃO houve resposta
    } finally {
      setCarregando(false);                       // roda no sucesso, no return e no catch
    }
  }

  // AULA 7 — NOVO: filtro novo sempre volta para a página 1
  function aplicarFiltro(evento) {
    evento.preventDefault();
    buscarRecursos(1);
  }

  // WEB 9 — NOVO: envia usuário e senha; se der certo, guarda a sessão.
  async function entrar(evento) {
    evento.preventDefault();

    const resposta = await fetch('http://localhost:5137/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ usuario: loginUsuario, senha: loginSenha })
    });

    if (!resposta.ok) {
      setErroLogin(await lerProblema(resposta));   // "Usuário ou senha inválidos"
      return;
    }

    setSessao(await resposta.json());              // { token, nome, roles }
    setLoginSenha('');                             // não deixa a senha na tela
    setErroLogin(null);
  }

  // WEB 9 — NOVO: cria a conta com o usuário e a senha digitados e, se der certo, já entra.
  async function cadastrar() {
    const resposta = await fetch('http://localhost:5137/auth/cadastro', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ usuario: loginUsuario, senha: loginSenha })
    });

    if (!resposta.ok) {
      setErroLogin(await lerProblema(resposta));   // "Usuário já existe" ou as regras da senha
      return;
    }

    await entrar({ preventDefault() {} });         // reaproveita o login: mesmo usuário e senha
  }

  // WEB 9 — NOVO: sair = esquecer o token (o servidor não guarda sessão).
  function sair() {
    setSessao(null);
    cancelarEdicao();
  }

  // WEB 9 — NOVO: a API respondeu 401 -> token vencido ou inválido -> volta ao login.
  function sessaoExpirou() {
    setSessao(null);
    setErroLogin('Sua sessão expirou. Entre de novo.');
  }

  // WEB 9 — NOVO: cabeçalhos com o token, usados no POST, PUT e DELETE.
  function cabecalhosComToken() {
    const cabecalhos = { 'Content-Type': 'application/json' };
    if (sessao) {
      cabecalhos.Authorization = `Bearer ${sessao.token}`;
    }
    return cabecalhos;
  }

  // APLICADA 8 — NOVO: troca a situação de UM recurso, sem perder a dos outros.
  // A forma "atual => ..." usa o valor mais recente, mesmo com vários avisos seguidos.
  function atualizarAnalise(id, dados) {
    setAnalises(atual => ({
      ...atual,
      [id]: { etapa: dados.etapa, recomendacao: dados.recomendacao, erro: dados.erro }
    }));
  }

  // APLICADA 8 — NOVO: entra no grupo do recurso e só DEPOIS pede a análise.
  async function analisarRecurso(id) {
    const conexao = conexaoRef.current;
    acompanhados.current.add(id);

    // (1) Entrar no grupo ANTES do POST: assim nenhum aviso sai antes de estarmos ouvindo.
    if (conexao.state === EstadoDaConexao.Connected) {
      try {
        await conexao.invoke('AcompanharRecurso', id);   // chama o método do Hub
      } catch (e) {
        atualizarAnalise(id, { etapa: 'Falhou', erro: e.message });
        return;
      }
    }

    // (2) 'Enviando' marca que o pedido saiu e a resposta ainda não chegou.
    atualizarAnalise(id, { etapa: 'Enviando' });

    const resposta = await fetch(`http://localhost:5137/recursos/${id}/analise`, {
      method: 'POST'
    });

    if (!resposta.ok) {                                   // ex.: 503 fila cheia
      atualizarAnalise(id, { etapa: 'Recusada', erro: await lerProblema(resposta) });
      return;
    }

    // (3) O 202 diz "NaFila". Mas um aviso "Processando" pode ter chegado ANTES desta
    // resposta. Só gravamos "NaFila" se ainda estivermos em "Enviando".
    const aceito = await resposta.json();
    setAnalises(atual => atual[id]?.etapa === 'Enviando'
      ? { ...atual, [id]: { etapa: aceito.etapa } }
      : atual);
  }

  // APLICADA 8 — NOVO: depois de reconectar, a conexão é NOVA e não está em grupo nenhum.
  // Para cada recurso acompanhado: volta ao grupo e pergunta a situação atual por HTTP.
  async function reconciliar(conexao) {
    for (const id of acompanhados.current) {
      try {
        await conexao.invoke('AcompanharRecurso', id);
        const resposta = await fetch(`http://localhost:5137/recursos/${id}/analise`);

        if (resposta.ok) {
          atualizarAnalise(id, await resposta.json());
        } else if (resposta.status === 404) {
          // o status fica em memória (Aula 6): se a API reiniciou, ele se perdeu
          atualizarAnalise(id, { etapa: 'Desconhecida', erro: 'A API não tem mais esta análise.' });
        }
      } catch (e) {
        atualizarAnalise(id, { etapa: 'Desconhecida', erro: e.message });
      }
    }
  }

  // APLICADA 8 — NOVO: depois das 4 tentativas automáticas, a pessoa pode tentar de novo.
  async function reconectar() {
    const conexao = conexaoRef.current;
    setTempoReal('conectando');
    try {
      await conexao.start();
      setTempoReal('conectado');
      await reconciliar(conexao);
    } catch {
      setTempoReal('desconectado');
    }
  }

  // APLICADA 8 — NOVO: o texto que aparece ao lado do recurso.
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

  // Decide POST ou PUT, dependendo se estamos criando ou editando.
  async function salvarRecurso(evento) {
    evento.preventDefault();

    const corpo = { nome, capacidade: Number(capacidade) };

    const url = recursoEditando
      ? `http://localhost:5137/recursos/${recursoEditando}`
      : 'http://localhost:5137/recursos';
    const method = recursoEditando ? 'PUT' : 'POST';

    const resposta = await fetch(url, {
      method,
      headers: cabecalhosComToken(),              // WEB 9 — MUDOU: agora leva o token
      body: JSON.stringify(corpo)
    });

    if (resposta.status === 401) {                // WEB 9 — NOVO
      sessaoExpirou();
      return;
    }

    if (!resposta.ok) {
      // AULA 7 — MUDOU: mostra a mensagem que veio da API (ex.: "O nome é obrigatório.")
      setErroFormulario(await lerProblema(resposta));
      return;
    }

    setNome('');
    setCapacidade('');
    setErroFormulario(null);      // AULA 7 — NOVO: some a mensagem de erro anterior
    setRecursoEditando(null);     // volta ao modo "criar", depois de editar com sucesso
    buscarRecursos();             // recarrega a página ATUAL
  }

  // Preenche o formulário com os dados daquele recurso, e "liga" o modo edição.
  function iniciarEdicao(recurso) {
    setRecursoEditando(recurso.id);
    setNome(recurso.nome);
    setCapacidade(String(recurso.capacidade));
    setErroFormulario(null);
  }

  // Cancela a edição em andamento, sem salvar nada, voltando ao formulário vazio.
  function cancelarEdicao() {
    setRecursoEditando(null);
    setNome('');
    setCapacidade('');
    setErroFormulario(null);
  }

  // Remove um recurso, chamando o DELETE da API.
  async function removerRecurso(id) {
    const resposta = await fetch(`http://localhost:5137/recursos/${id}`, {
      method: 'DELETE',
      headers: cabecalhosComToken()               // WEB 9 — MUDOU: agora leva o token
    });

    if (resposta.status === 401) {                // WEB 9 — NOVO
      sessaoExpirou();
      return;
    }

    if (resposta.status === 403) {                // WEB 9 — NOVO
      setErro('Só administradores podem remover recursos.');
      return;
    }

    if (!resposta.ok) {
      setErro(await lerProblema(resposta));   // AULA 7 — MUDOU
      return;
    }

    // Se a pessoa estava editando justamente o item que acabou de remover,
    // cancela a edição, pra não deixar o formulário "preso" num id que não existe mais.
    if (recursoEditando === id) {
      cancelarEdicao();
    }

    buscarRecursos();
  }

  return (
    <div>
      <h1>Recursos</h1>

      {/* WEB 9 — NOVO: logado mostra o nome e "Sair"; senão, o formulário de login */}
      {sessao ? (
        <p>
          Olá, {sessao.nome} ({sessao.roles.join(', ')})
          <button onClick={sair}>Sair</button>
        </p>
      ) : (
        <form onSubmit={entrar}>
          <input
            placeholder="Usuário"
            value={loginUsuario}
            onChange={e => setLoginUsuario(e.target.value)}
          />
          <input
            type="password"
            placeholder="Senha"
            value={loginSenha}
            onChange={e => setLoginSenha(e.target.value)}
          />
          <button type="submit">Entrar</button>
          <button type="button" onClick={cadastrar}>Cadastrar</button>   {/* WEB 9 — NOVO */}
          {erroLogin && <p>{erroLogin}</p>}
        </form>
      )}

      {/* APLICADA 8 — NOVO: estado da conexão em tempo real */}
      <p>
        Tempo real: {tempoReal}
        {tempoReal === 'desconectado' && (
          <button onClick={reconectar}>Reconectar</button>
        )}
      </p>

      {/* AULA 7 — NOVO: filtro por nome */}
      <form onSubmit={aplicarFiltro}>
        <input
          placeholder="Filtrar por nome"
          value={filtroNome}
          onChange={e => setFiltroNome(e.target.value)}
        />
        <button type="submit">Filtrar</button>
      </form>

      {carregando && <p>Carregando...</p>}
      {erro && <p>Erro: {erro}</p>}
      {!carregando && !erro && recursos.length === 0 && <p>Nenhum recurso nesta página.</p>}

      <ul>
        {recursos.map(r => (
          <li key={r.id}>
            {r.nome} — capacidade: {r.capacidade}
            {/* WEB 9 — MUDOU: botões só aparecem para quem pode usá-los */}
            {sessao && <button onClick={() => iniciarEdicao(r)}>Editar</button>}
            {ehAdministrador && <button onClick={() => removerRecurso(r.id)}>Remover</button>}
            {/* APLICADA 8 — NOVO: pede a análise e mostra a situação, que muda sozinha (livre, sem login) */}
            <button
              onClick={() => analisarRecurso(r.id)}
              disabled={['Enviando', 'NaFila', 'Processando'].includes(analises[r.id]?.etapa)}
            >
              Analisar
            </button>
            <span> {textoDaAnalise(analises[r.id])}</span>
          </li>
        ))}
      </ul>

      {/* AULA 7 — NOVO: navegação entre páginas.
          Veio MENOS que TAMANHO? Então é a última página: "Próxima" desliga. */}
      <div>
        <button onClick={() => buscarRecursos(pagina - 1)} disabled={pagina <= 1 || carregando}>
          Anterior
        </button>
        <span> Página {pagina} </span>
        <button onClick={() => buscarRecursos(pagina + 1)} disabled={recursos.length < TAMANHO || carregando}>
          Próxima
        </button>
      </div>

      {/* WEB 9 — MUDOU: o formulário só aparece para quem está logado */}
      {sessao && (
        <div>
          <h2>{recursoEditando ? 'Editar recurso' : 'Novo recurso'}</h2>
          <form onSubmit={salvarRecurso}>
            <input
              value={nome}
              onChange={e => setNome(e.target.value)}
            />
            <input
              type="number"
              value={capacidade}
              onChange={e => setCapacidade(e.target.value)}
            />
            <button type="submit">
              {recursoEditando ? 'Salvar edição' : 'Criar'}
            </button>
            {recursoEditando && (
              <button type="button" onClick={cancelarEdicao}>
                Cancelar
              </button>
            )}
            {erroFormulario && <p>{erroFormulario}</p>}
          </form>
        </div>
      )}
    </div>
  );
}

export default App;
