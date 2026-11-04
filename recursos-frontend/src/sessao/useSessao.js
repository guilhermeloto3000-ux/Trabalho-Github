// sessao/useSessao.js — AULA FINAL — NOVO
// O Context (a "caixa" que guarda a sessão) e o hook que as telas usam para lê-la.
// Fica separado do SessaoProvider.jsx porque o Vite só recarrega rápido (Fast Refresh)
// arquivos .jsx que exportam apenas componentes.
import { createContext, useContext } from 'react';

export const SessaoContext = createContext(null);

// Em qualquer tela: const { sessao, encerrarSessao } = useSessao();
export function useSessao() {
  return useContext(SessaoContext);
}
