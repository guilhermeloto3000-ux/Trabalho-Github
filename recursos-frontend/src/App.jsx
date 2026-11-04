// App.jsx — AULA FINAL — MUDOU
// Antes: 457 linhas, com o login, o cadastro e a manutenção de recursos juntos.
// Agora: só o cabeçalho e as ROTAS. Cada rota mostra uma tela da pasta telas/.
//   /          -> Recursos  (lista para todos; criar/editar para quem entrou; remover para admin)
//   /login     -> Login
//   /cadastro  -> Cadastro
// Nenhuma rota do React é igual a uma rota GET da API (/recursos, /fila/status...):
// publicado, o mesmo servidor responde as duas.
import { Routes, Route, Link, useNavigate } from 'react-router';
import './App.css';
import { useSessao } from './sessao/useSessao.js';
import Recursos from './telas/Recursos.jsx';
import Login from './telas/Login.jsx';
import Cadastro from './telas/Cadastro.jsx';

function App() {
  const { sessao, encerrarSessao } = useSessao();
  const navegar = useNavigate();

  // sair = esquecer o token (o servidor não guarda sessão) e voltar para a lista
  function sair() {
    encerrarSessao();
    navegar('/');
  }

  return (
    <div>
      <h1>Recursos</h1>

      {/* Link troca de tela SEM recarregar a página (a sessão na memória continua) */}
      <nav>
        <Link to="/">Recursos</Link>{' '}
        {sessao ? (
          <span>
            Olá, {sessao.nome} ({sessao.roles.join(', ')})
            <button onClick={sair}>Sair</button>
          </span>
        ) : (
          <span>
            <Link to="/login">Entrar</Link>{' '}
            <Link to="/cadastro">Cadastrar</Link>
          </span>
        )}
      </nav>

      <Routes>
        <Route path="/" element={<Recursos />} />
        <Route path="/login" element={<Login />} />
        <Route path="/cadastro" element={<Cadastro />} />
        <Route path="*" element={<p>Página não encontrada.</p>} />
      </Routes>
    </div>
  );
}

export default App;
