# SoluçãoDotNet — RecursosApi + recursos-frontend

Sistema de cadastro de recursos (salas, laboratórios) com:
- login e cadastro de usuários (Identity + JWT);
- autorização por role: Usuario cria e edita, Administrador também remove;
- análise de recursos numa fila em segundo plano, com aviso em tempo real (SignalR).

| Pasta | O que é |
|---|---|
| `RecursosApi/` | a API REST em ASP.NET Core (.NET 10), com o banco SQLite |
| `recursos-frontend/` | o React (Vite), que conversa com a API por `fetch` |
| `RecursosApi.Tests/` | os testes automatizados (xUnit) |
| `Dockerfile` | a receita de publicação no Render (veja `PUBLICAR.md`) |
| `.github/workflows/ci.yml` | o CI: roda os testes a cada push |

**Publicado em:** https://SEU-SERVICO.onrender.com  ← troque pelo seu endereço

---

## Como rodar na sua máquina

### 0. Precisa ter instalado

- **.NET 10 SDK.** Confira com `dotnet --version`, que deve mostrar 10.x.
- **Node.js 22** (LTS). Confira com `node --version`.
- **VS Code**, com as extensões **C#** e **REST Client**.

### 1. Instalar as dependências (uma vez)

No terminal, na pasta `SolucaoDotNet`:

```powershell
dotnet restore
npm install
npm install --prefix recursos-frontend
```

### 2. Configurar os segredos (uma vez, em cada máquina)

Os segredos **não vão para o GitHub**. Cada um grava os seus, **dentro da pasta `RecursosApi`**:

```powershell
cd RecursosApi
dotnet user-secrets init

$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$chave = [Convert]::ToBase64String($bytes)
dotnet user-secrets set "Jwt:Chave" $chave

dotnet user-secrets set "SeedSenhas:Ana" "Senha.Ana123"
dotnet user-secrets set "SeedSenhas:Admin" "Senha.Admin123"
dotnet user-secrets list
cd ..
```

### 3. Rodar

```powershell
npm run dev
```

Espere aparecer `Now listening on: http://localhost:5137` e abra **http://localhost:5173**.

Não é mais preciso rodar `dotnet ef database update`: a API aplica as migrations sozinha ao subir.
O `dotnet ef migrations add` continua sendo usado quando você **muda** o modelo (uma classe nova, uma coluna nova).

### 4. Testes

```powershell
dotnet test        # esperado: 20 aprovados
```

---

## Como o projeto está organizado

**API** — uma pasta por assunto. Cada pasta tem um `XxxModulo.cs` que registra os seus serviços (`AddXxx`), e os endpoints ficam em `XxxEndpoints.cs` (`MapXxx`).

```
RecursosApi/
├── Recursos/        Recurso, DTOs, RecursoService, ValidadorDeRecurso, RecursosEndpoints, RecursosApiController
├── Recomendacao/    classificador, catálogo, recomendadores, repositório, RecomendacaoService
├── Analises/        fila, worker, status, AnaliseHub, AnaliseEndpoints
├── Autenticacao/    TokenService, SeedDeUsuarios, AuthEndpoints
├── Dados/           RecursosApiContext e a aplicação das migrations
├── Infraestrutura/  middlewares, CORS, Swagger, Problem Details, /health
├── Migrations/      geradas pelo dotnet ef (ficam na raiz, onde o dotnet ef as procura)
└── Program.cs       chama os AddXxx, monta o pipeline e chama os MapXxx
```

**React** — uma tela por rota (React Router):

```
recursos-frontend/src/
├── api/            recursosApi.js (todas as chamadas REST) e lerProblema.js
├── sessao/         SessaoProvider.jsx e useSessao.js (quem está logado)
├── telas/          Recursos.jsx (/), Login.jsx (/login), Cadastro.jsx (/cadastro)
├── componentes/    ListaDeRecursos, FormularioRecurso, Paginacao, StatusTempoReal
├── tempoReal/      conexaoAnalises.js e useAnalises.js (SignalR)
└── App.jsx         o cabeçalho e as rotas
```

---

## Se der erro

| O que aparece | Causa | O que fazer |
|---|---|---|
| `'vite' não é reconhecido` | faltou o `npm install` do front | passo 1 |
| `Could not find a MSBuild project file` | comando de user-secrets fora da pasta | `cd RecursosApi` antes |
| Tudo responde **500**; no terminal, "Configure Jwt:Chave" | chave ausente ou curta | refaça o passo 2 e rode de novo |
| "Não foi possível conectar à API" | a página abriu antes de a API subir | espere o `Now listening` e aperte F5 |
| "Usuário ana não criado" no terminal | faltou a senha no User Secrets | passo 2 |
