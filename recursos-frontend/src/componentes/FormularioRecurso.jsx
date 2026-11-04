// componentes/FormularioRecurso.jsx — AULA FINAL — NOVO (o código veio do App.jsx: salvarRecurso)
// recurso = null  -> "Novo recurso" (POST)
// recurso = {...} -> "Editar recurso" (PUT), com os campos já preenchidos
// A tela usa key={...} neste componente: quando o recurso muda, o React cria o formulário
// de novo e os campos começam com os valores certos.
import { useState } from 'react';
import { salvarRecurso } from '../api/recursosApi.js';
import { lerProblema } from '../api/lerProblema.js';

function FormularioRecurso({ recurso, token, onSalvo, onCancelar, onSessaoExpirou }) {
  const [nome, setNome] = useState(recurso?.nome ?? '');
  const [capacidade, setCapacidade] = useState(recurso ? String(recurso.capacidade) : '');
  const [erro, setErro] = useState(null);

  async function enviar(evento) {
    evento.preventDefault();

    const resposta = await salvarRecurso(recurso?.id, { nome, capacidade: Number(capacidade) }, token);

    if (resposta.status === 401) {     // token vencido ou inválido
      onSessaoExpirou();
      return;
    }
    if (!resposta.ok) {
      setErro(await lerProblema(resposta));   // ex.: "O nome é obrigatório."
      return;
    }

    setNome('');
    setCapacidade('');
    setErro(null);
    onSalvo();                          // a tela recarrega a lista
  }

  return (
    <div>
      <h2>{recurso ? 'Editar recurso' : 'Novo recurso'}</h2>
      <form onSubmit={enviar}>
        <input placeholder="Nome" value={nome} onChange={e => setNome(e.target.value)} />
        <input type="number" placeholder="Capacidade" value={capacidade} onChange={e => setCapacidade(e.target.value)} />
        <button type="submit">{recurso ? 'Salvar edição' : 'Criar'}</button>
        {recurso && <button type="button" onClick={onCancelar}>Cancelar</button>}
        {erro && <p>{erro}</p>}
      </form>
    </div>
  );
}

export default FormularioRecurso;
