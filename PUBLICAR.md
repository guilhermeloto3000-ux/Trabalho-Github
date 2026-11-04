# Como publicar no Render (gratuito)

O resultado é um endereço público, por exemplo `https://recursos-ana.onrender.com`, que abre o sistema em qualquer navegador.

**O que o plano gratuito do Render faz:**
- Não pede cartão de crédito.
- **Dorme depois de 15 minutos sem acesso.** O primeiro acesso depois disso demora cerca de 1 minuto. Antes de apresentar, abra o link e espere carregar.
- **Apaga o banco a cada reinício ou nova publicação.** A API recria sozinha as tabelas, as roles e o usuário admin. Recursos e usuários cadastrados somem.

Isso basta para apresentar o trabalho. Para um sistema de verdade, o banco ficaria num serviço separado, e o servidor, num plano pago.

---

## 1. Colocar o projeto no GitHub

### 1.1 Conferir o `.gitignore`

O `.gitignore` na pasta `SolucaoDotNet` impede que vão para o GitHub:
- `bin/` e `obj/`;
- `node_modules/`;
- o `recursos.db`, que tem os hashes das senhas.

### 1.2 Criar o repositório local

Na pasta `SolucaoDotNet`:

```powershell
git init
git add .
git status          # NÃO pode aparecer bin/, obj/, node_modules/ nem recursos.db
git commit -m "Projeto do semestre"
```

Se o `recursos.db` aparecer na lista, ele já tinha sido adicionado antes. Para tirar:

```powershell
git rm --cached RecursosApi/recursos.db
```

### 1.3 Enviar para o GitHub

No GitHub, clique em **New repository** e crie o repositório **sem README**. Depois copie os comandos que ele mostra:

```powershell
git remote add origin https://github.com/SEU-USUARIO/SEU-REPOSITORIO.git
git branch -M main
git push -u origin main
```

### 1.4 Conferir o CI

Abra a aba **Actions** do repositório. O workflow **CI** roda sozinho e precisa terminar com ✔ verde, com os 20 testes aprovados e o React compilado.

## 2. Criar o serviço no Render

1. Entre em https://render.com e faça login com o **GitHub**.
2. Clique em **New → Web Service** e escolha o seu repositório. Se ele não aparecer, autorize o Render a ver o repositório.
3. Preencha:
   - **Language:** Docker. O Render lê o `Dockerfile` da raiz.
   - **Branch:** `main`.
   - **Region:** a mais próxima, por exemplo Virginia (US East).
   - **Instance Type:** **Free**.
4. Em **Environment Variables**, crie duas:

   | Nome | Valor |
   |---|---|
   | `Jwt__Chave` | clique em **Generate**, e o Render cria uma chave aleatória. Se o botão não aparecer, rode as 3 primeiras linhas da chave do passo 2 do README, depois `$chave` para mostrar o valor, e cole |
   | `SeedSenhas__Admin` | uma senha forte, por exemplo `Senha.Admin2026` |

   O nome usa `__` (dois sublinhados) no lugar do `:` do User Secrets. O .NET lê as duas formas do mesmo jeito.
5. Em **Advanced**:
   - **Health Check Path:** `/health`.
   - **Auto-Deploy:** **After CI Checks Pass**. Só publica quando o CI está verde.
6. Clique em **Create Web Service**.

A primeira publicação leva alguns minutos. Nos **Logs** devem aparecer:
- `CREATE TABLE` (as migrations);
- `Now listening on`, com a porta 10000.

O endereço aparece no topo da página, terminado em `.onrender.com`.

## 3. Testar o endereço público

Troque `SEU-SERVICO` pelo nome do seu serviço:

- `https://SEU-SERVICO.onrender.com/health` deve mostrar `Healthy`.
- `https://SEU-SERVICO.onrender.com` deve mostrar a tela de recursos com "Tempo real: conectado".
- Entre com `admin` e a senha do `SeedSenhas__Admin`.
- Cadastre um usuário novo, crie um recurso e peça a análise.

Para testar com os arquivos `.http`, troque a primeira linha por `@api = https://SEU-SERVICO.onrender.com`.

## 4. Publicar uma mudança

```powershell
git add .
git commit -m "O que mudou"
git push
```

O que acontece depois:
1. O GitHub roda o CI.
2. Se o CI ficar verde, o Render publica sozinho.
3. Se um teste falhar, o commit fica com ✗ vermelho e **o site continua na versão anterior**.

## Se der erro no Render

| O que aparece nos Logs | Causa | O que fazer |
|---|---|---|
| `Configure Jwt:Chave` (e o /health responde 500) | faltou a variável `Jwt__Chave`, ou o nome está errado (um `_` só) | **Environment**, corrigir e **Save** |
| `Usuário admin não criado` | faltou `SeedSenhas__Admin` | idem |
| `Não foi possível criar admin: Passwords must...` | a senha do admin é fraca | 8+ caracteres, com maiúscula, minúscula, número e símbolo |
| `npm ci` falhou | `package-lock.json` desatualizado | `npm install --prefix recursos-frontend`, commit e push |
| O deploy não começa | o CI está vermelho, ou o Auto-Deploy espera o CI | aba Actions do GitHub |
| A página demora 1 minuto | o serviço estava dormindo | normal no plano gratuito |

---

**E o Azure?** O Azure App Service também publica este projeto, e o banco não se apaga. O acesso gratuito para estudantes, o **Azure for Students**, exige verificar o e-mail da instituição. Ainda não confirmamos se o e-mail da UNIP é aceito. Quem conseguir pode usar o Azure no lugar do Render: o `.gitignore`, o CI e a preparação da API para produção são os mesmos.
