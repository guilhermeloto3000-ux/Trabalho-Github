// tempoReal/useAnalises.js — AULA FINAL — NOVO (o código veio do App.jsx, APLICADA 8)
// Um "hook" próprio: uma função que começa com "use" e usa outros hooks (useState, useEffect...).
// Junta tudo o que é do tempo real: a conexão SignalR, a situação de cada análise,
// a reconciliação depois de reconectar. A tela só chama:
//   const { analises, tempoReal, analisarRecurso, reconectar } = useAnalises();
import { useState, useEffect, useRef } from 'react';
import { criarConexaoAnalises, EstadoDaConexao } from './conexaoAnalises.js';
import { pedirAnalise, obterAnalise } from '../api/recursosApi.js';
import { lerProblema } from '../api/lerProblema.js';

export function useAnalises() {
  // situação da análise de cada recurso. Ex.: { 3: { etapa: 'Processando' } }
  const [analises, setAnalises] = useState({});
  // estado da conexão em tempo real, para mostrar na tela
  const [tempoReal, setTempoReal] = useState('conectando');

  const conexaoRef = useRef(null);            // a conexão SignalR
  const acompanhados = useRef(new Set());     // ids dos recursos que esta aba acompanha

  // troca a situação de UM recurso, sem perder a dos outros
  function atualizarAnalise(id, dados) {
    setAnalises(atual => ({
      ...atual,
      [id]: { etapa: dados.etapa, recomendacao: dados.recomendacao, erro: dados.erro }
    }));
  }

  // depois de reconectar, a conexão é NOVA e não está em grupo nenhum:
  // volta a cada grupo e pergunta a situação atual por HTTP
  async function reconciliar(conexao) {
    for (const id of acompanhados.current) {
      try {
        await conexao.invoke('AcompanharRecurso', id);
        const resposta = await obterAnalise(id);       // GET /recursos/{id}/analise

        if (resposta.ok) {
          atualizarAnalise(id, await resposta.json());
        } else if (resposta.status === 404) {
          atualizarAnalise(id, { etapa: 'Desconhecida', erro: 'A API não tem mais esta análise.' });
        }
      } catch (e) {
        atualizarAnalise(id, { etapa: 'Desconhecida', erro: e.message });
      }
    }
  }

  // abre a conexão com o Hub quando a tela aparece e fecha quando ela some
  useEffect(() => {
    const conexao = criarConexaoAnalises();
    conexaoRef.current = conexao;
    let ativa = true;

    conexao.on('AnaliseAtualizada', evento => atualizarAnalise(evento.recursoId, evento));
    conexao.onreconnecting(() => ativa && setTempoReal('reconectando'));
    conexao.onreconnected(async () => {
      if (!ativa) return;
      setTempoReal('conectado');
      await reconciliar(conexao);
    });
    conexao.onclose(() => ativa && setTempoReal('desconectado'));

    conexao.start()
      .then(() => ativa && setTempoReal('conectado'))
      .catch(() => ativa && setTempoReal('desconectado'));

    return () => {
      ativa = false;
      conexao.stop();
    };
  }, []);

  // entra no grupo do recurso e só DEPOIS pede a análise
  async function analisarRecurso(id) {
    const conexao = conexaoRef.current;
    acompanhados.current.add(id);

    if (conexao.state === EstadoDaConexao.Connected) {
      try {
        await conexao.invoke('AcompanharRecurso', id);
      } catch (e) {
        atualizarAnalise(id, { etapa: 'Falhou', erro: e.message });
        return;
      }
    }

    atualizarAnalise(id, { etapa: 'Enviando' });

    const resposta = await pedirAnalise(id);            // POST /recursos/{id}/analise
    if (!resposta.ok) {                                 // ex.: 503 fila cheia
      atualizarAnalise(id, { etapa: 'Recusada', erro: await lerProblema(resposta) });
      return;
    }

    // um aviso "Processando" pode ter chegado ANTES desta resposta
    const aceito = await resposta.json();
    setAnalises(atual => atual[id]?.etapa === 'Enviando'
      ? { ...atual, [id]: { etapa: aceito.etapa } }
      : atual);
  }

  // depois das tentativas automáticas, a pessoa pode tentar de novo
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

  return { analises, tempoReal, analisarRecurso, reconectar };
}
