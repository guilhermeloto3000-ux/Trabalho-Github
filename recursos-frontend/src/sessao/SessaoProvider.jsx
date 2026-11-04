// sessao/SessaoProvider.jsx — AULA FINAL — NOVO
// Antes, a sessão era um useState dentro do App.jsx, e só o App.jsx enxergava.
// Agora são três telas (Login, Cadastro, Recursos) que precisam da MESMA sessão.
// Um Context do React guarda um valor que qualquer componente dentro do Provider lê,
// sem passar o valor de componente em componente.
import { useState } from 'react';
import { SessaoContext } from './useSessao.js';

// Envolve o app inteiro (veja main.jsx).
export function SessaoProvider({ children }) {
  // o que o login devolveu ({ token, nome, roles }); null = ninguém logado.
  // Continua só na memória: some ao recarregar a página (F5), como antes.
  const [sessao, setSessao] = useState(null);

  const valor = {
    sessao,
    iniciarSessao: setSessao,          // chamado pela tela de Login depois do 200
    encerrarSessao: () => setSessao(null),
    // só serve para esconder botões; quem decide de verdade é a API
    ehAdministrador: sessao !== null && sessao.roles.includes('Administrador')
  };

  return <SessaoContext.Provider value={valor}>{children}</SessaoContext.Provider>;
}
