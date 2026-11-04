import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router'                  // AULA FINAL — NOVO
import './index.css'
import App from './App.jsx'
import { SessaoProvider } from './sessao/SessaoProvider.jsx'   // AULA FINAL — NOVO

// AULA FINAL — MUDOU:
//   BrowserRouter  = lê o endereço do navegador (/login, /cadastro...) e escolhe a tela;
//   SessaoProvider = a sessão fica disponível para todas as telas.
createRoot(document.getElementById('root')).render(
  <StrictMode>
    <BrowserRouter>
      <SessaoProvider>
        <App />
      </SessaoProvider>
    </BrowserRouter>
  </StrictMode>,
)
