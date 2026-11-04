// telas/Cadastro.jsx — AULA FINAL — NOVO (o código veio do App.jsx: cadastrar)
// Antes, o cadastro usava os mesmos campos do login. Agora é uma tela própria,
// com a confirmação da senha (conferida aqui mesmo, antes de chamar a API).
import { useState } from 'react';
import { useNavigate, Link } from 'react-router';
import { cadastrar, entrar } from '../api/recursosApi.js';
import { lerProblema } from '../api/lerProblema.js';
import { useSessao } from '../sessao/useSessao.js';

function Cadastro() {
  const { iniciarSessao } = useSessao();
  const navegar = useNavigate();

  const [usuario, setUsuario] = useState('');
  const [senha, setSenha] = useState('');
  const [confirmacao, setConfirmacao] = useState('');
  const [erro, setErro] = useState(null);

  async function enviar(evento) {
    evento.preventDefault();

    if (senha !== confirmacao) {
      setErro('As senhas não conferem.');
      return;
    }

    const resposta = await cadastrar(usuario, senha);   // POST /auth/cadastro
    if (!resposta.ok) {
      setErro(await lerProblema(resposta));             // "Usuário já existe" ou as regras da senha
      return;
    }

    // deu certo (201): já faz o login com o mesmo usuário e senha
    const login = await entrar(usuario, senha);
    if (!login.ok) {
      navegar('/login');
      return;
    }
    iniciarSessao(await login.json());
    navegar('/');
  }

  return (
    <div>
      <h2>Cadastrar</h2>
      <form onSubmit={enviar}>
        <input placeholder="Usuário" value={usuario} onChange={e => setUsuario(e.target.value)} />
        <input type="password" placeholder="Senha" value={senha} onChange={e => setSenha(e.target.value)} />
        <input type="password" placeholder="Repita a senha" value={confirmacao} onChange={e => setConfirmacao(e.target.value)} />
        <button type="submit">Cadastrar</button>
        {erro && <p>{erro}</p>}
      </form>
      <p>A senha precisa de 8 caracteres, com maiúscula, minúscula, número e símbolo.</p>
      <p>Já tem conta? <Link to="/login">Entrar</Link></p>
    </div>
  );
}

export default Cadastro;
