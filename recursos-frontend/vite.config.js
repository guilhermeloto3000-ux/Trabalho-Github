import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// AULA FINAL — NOVO: proxy de desenvolvimento.
// O navegador fala só com o Vite (http://localhost:5173). Quando o pedido começa com
// um destes caminhos, o Vite o repassa para a API (http://localhost:5137).
// Assim o código do React usa endereços relativos ('/recursos'), iguais aos de produção,
// onde a própria API entrega o React e não existe Vite.
const api = 'http://localhost:5137'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/recursos': api,
      '/auth': api,
      '/fila': api,
      '/health': api,
      '/hubs': { target: api, ws: true },   // ws: true = repassa também o WebSocket do SignalR
    },
  },
})
