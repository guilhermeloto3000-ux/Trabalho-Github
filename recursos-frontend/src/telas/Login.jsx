// telas/Login.jsx — AULA FINAL — NOVO (o código veio do App.jsx: entrar)
import { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router';
import { entrar } from '../api/recursosApi.js';
import { lerProblema } from '../api/lerProblema.js';
import { useSessao } from '../sessao/useSessao.js';

function Login() {
  const { iniciarSessao } = useSessao();
  const navegar = useNavigate();
  // aviso enviado por outra tela (ex.: "Sua sessão expirou") em navegar('/login', { state })
  const aviso = useLocation().state?.aviso;

  const [usuario, setUsuario] = useState('');
  const [senha, setSenha] = useState('');
  const [erro, setErro] = useState(null);

  async function enviar(evento) {
    evento.preventDefault();

    const resposta = await entrar(usuario, senha);   // POST /auth/login

    if (!resposta.ok) {
      setErro(await lerProblema(resposta));          // "Usuário ou senha inválidos"
      return;
    }

    iniciarSessao(await resposta.json());            // { token, nome, roles }
    navegar('/');                                    // vai para a tela de recursos
  }

  return (
    <div>
      <h2>Entrar</h2>
      {aviso && <p>{aviso}</p>}
      <form onSubmit={enviar}>
        <input placeholder="Usuário" value={usuario} onChange={e => setUsuario(e.target.value)} />
        <input type="password" placeholder="Senha" value={senha} onChange={e => setSenha(e.target.value)} />
        <button type="submit">Entrar</button>
        {erro && <p>{erro}</p>}
      </form>
      <p>Não tem conta? <Link to="/cadastro">Cadastre-se</Link></p>
    </div>
  );
}

export default Login;
