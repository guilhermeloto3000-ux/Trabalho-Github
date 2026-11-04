# Dockerfile — AULA FINAL — NOVO
# A "receita" que o Render segue para montar e rodar o sistema num servidor Linux.
# Cada FROM começa uma etapa a partir de uma imagem pronta. Só a ÚLTIMA vai para o servidor.

# ---------- Etapa 1: publicar o React ----------
FROM node:22-alpine AS front
WORKDIR /front
COPY recursos-frontend/package.json recursos-frontend/package-lock.json ./
RUN npm ci                          # instala exatamente as versões do package-lock.json
COPY recursos-frontend/ ./
RUN npm run build                   # gera /front/dist (index.html, .js, .css)

# ---------- Etapa 2: publicar a API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY RecursosApi/ RecursosApi/
RUN dotnet publish RecursosApi -c Release -o /publicado
# o React publicado vai para a wwwroot da API: um endereço só
COPY --from=front /front/dist /publicado/wwwroot

# ---------- Etapa 3: o que roda no servidor (sem SDK, sem Node) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /publicado .

# Configuração por variável de ambiente ("__" no lugar de ":" do appsettings.json)
ENV ASPNETCORE_HTTP_PORTS=10000
ENV ConnectionStrings__RecursosDb="Data Source=/tmp/recursos.db"
# 10000 = a porta que o Render espera (variável PORT). /tmp = pasta onde dá para gravar.
# Jwt__Chave e SeedSenhas__Admin NÃO ficam aqui: são segredos, configurados no painel do Render.

USER $APP_UID
ENTRYPOINT ["dotnet", "RecursosApi.dll"]
