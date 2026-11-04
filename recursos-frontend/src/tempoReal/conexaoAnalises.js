// tempoReal/conexaoAnalises.js — APLICADA 8 (AULA FINAL — MUDOU DE LUGAR: estava em src/)
// Monta a conexão SignalR com o Hub de análises da API.
// Instalar uma vez, na pasta recursos-frontend:  npm install @microsoft/signalr
import * as signalR from '@microsoft/signalr';

export function criarConexaoAnalises() {
  return new signalR.HubConnectionBuilder()
    // o mesmo endereço do app.MapHub<AnaliseHub>("/hubs/analises") da API
    // AULA FINAL — MUDOU: endereço relativo (sem http://localhost:5137), como no api/recursosApi.js
    .withUrl('/hubs/analises')
    // se a conexão cair, tenta de novo sozinho: logo, depois de 2 s, 10 s e 30 s
    .withAutomaticReconnect()
    // mostra no console do navegador (F12) o que a conexão está fazendo
    .configureLogging(signalR.LogLevel.Information)
    .build();
}

// Os estados possíveis da conexão (Connected, Reconnecting, Disconnected...).
export const EstadoDaConexao = signalR.HubConnectionState;
