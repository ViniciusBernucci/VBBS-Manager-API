# Docker — Guia Completo do VBBS Manager

> Este documento explica os conceitos fundamentais do Docker e como eles se aplicam especificamente neste projeto. Foi escrito para quem está aprendendo a plataforma enquanto constrói um sistema real.

---

## Índice

1. [O problema que o Docker resolve](#1-o-problema-que-o-docker-resolve)
2. [Conceitos fundamentais](#2-conceitos-fundamentais)
3. [Imagens e Dockerfile](#3-imagens-e-dockerfile)
4. [Multi-stage build — por que usamos](#4-multi-stage-build--por-que-usamos)
5. [Docker Compose — orquestrando múltiplos containers](#5-docker-compose--orquestrando-múltiplos-containers)
6. [Arquitetura desta solução](#6-arquitetura-desta-solução)
7. [Redes internas e comunicação entre serviços](#7-redes-internas-e-comunicação-entre-serviços)
8. [Volumes e persistência de dados](#8-volumes-e-persistência-de-dados)
9. [Variáveis de ambiente e segredos](#9-variáveis-de-ambiente-e-segredos)
10. [O fluxo completo — do código ao container](#10-o-fluxo-completo--do-código-ao-container)
11. [Como funciona no ambiente local](#11-como-funciona-no-ambiente-local)
12. [Como funciona em produção](#12-como-funciona-em-produção)
13. [Comandos do dia a dia](#13-comandos-do-dia-a-dia)
14. [O que acontece quando você executa `docker compose up`](#14-o-que-acontece-quando-você-executa-docker-compose-up)
15. [Decisões de arquitetura deste projeto](#15-decisões-de-arquitetura-deste-projeto)

---

## 1. O problema que o Docker resolve

Antes do Docker, quando você precisava rodar um projeto em outra máquina (ou no servidor de produção), era comum o cenário:

> "Na minha máquina funciona."

Isso acontecia porque cada máquina tem versões diferentes de Node.js, .NET, PostgreSQL, bibliotecas do sistema operacional, variáveis de ambiente configuradas de formas diferentes. O ambiente de desenvolvimento nunca era idêntico ao de produção.

### O que Docker faz

Docker empacota a aplicação **junto com todo o ambiente que ela precisa** — sistema operacional base, runtime, dependências, configurações — em uma unidade chamada **container**. Esse container executa de forma idêntica em qualquer máquina que tenha Docker instalado: no seu Mac, no Ubuntu do colega, na VPS da Hostinger.

A analogia mais usada: um container é como um **navio cargueiro de fretes**. Independente do que está dentro da caixa (Node.js, .NET, Postgres), o navio (Docker) sabe como transportar e descarregar qualquer caixa que siga o padrão.

---

## 2. Conceitos fundamentais

### Imagem (Image)

Uma imagem é um **pacote somente-leitura** que contém tudo necessário para rodar uma aplicação:

- Sistema operacional base (geralmente uma versão mínima do Linux)
- Runtime instalado (.NET, Node.js, etc.)
- Código da aplicação
- Configurações

Pense em uma imagem como uma **receita de bolo** ou um **snapshot de um ambiente**. Ela não executa sozinha — ela é usada para criar containers.

Imagens são empilhadas em camadas (*layers*). Cada instrução no Dockerfile cria uma nova camada. Isso permite reutilização: se dois projetos usam a mesma base (`dotnet/aspnet:8.0`), essa camada é compartilhada em disco, sem duplicação.

### Container

Um container é uma **instância em execução de uma imagem**. É a imagem "ganhando vida".

```
Imagem  →  instanciar  →  Container (processo rodando)
(receita)                  (bolo pronto)
```

Você pode criar múltiplos containers a partir da mesma imagem. Cada container é isolado dos outros e do sistema operacional do host. Se um container travar ou for deletado, os outros não são afetados.

Containers são **efêmeros por natureza**: quando um container é removido, tudo que foi escrito dentro dele (arquivos, dados) é perdido — a menos que você use volumes (explicado adiante).

### Registry

Um registry é um repositório de imagens, como o GitHub para código. O principal é o **Docker Hub** (hub.docker.com). É de lá que vêm imagens como:

- `postgres:16-alpine` — PostgreSQL 16 em Alpine Linux
- `mcr.microsoft.com/dotnet/aspnet:8.0` — Runtime .NET 8
- `nginx:1.27-alpine` — Nginx em Alpine Linux
- `node:22-alpine` — Node.js 22 em Alpine Linux

Quando você faz `docker compose up` pela primeira vez, o Docker **baixa** as imagens que ainda não estão no seu computador. Nas próximas vezes, usa o cache local.

### Alpine Linux

Você vai ver `:alpine` em muitas imagens. Alpine Linux é uma distribuição Linux **minimalista** (~5MB), muito usada em containers porque resulta em imagens menores e com menor superfície de ataque para segurança. A imagem `postgres:16-alpine` tem ~80MB, enquanto `postgres:16` (Debian) tem ~400MB.

---

## 3. Imagens e Dockerfile

O `Dockerfile` é o arquivo que define como uma imagem é construída. É uma sequência de instruções que o Docker executa em ordem.

### Dockerfile da API (`.NET 8`)

```dockerfile
# Arquivo: API/Dockerfile

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY VBBSManager.sln .
COPY src/VBBSManager.Api/VBBSManager.Api.csproj             src/VBBSManager.Api/
COPY src/VBBSManager.Domain/VBBSManager.Domain.csproj       src/VBBSManager.Domain/
COPY src/VBBSManager.Infrastructure/VBBSManager.Infrastructure.csproj src/VBBSManager.Infrastructure/
COPY tests/VBBSManager.Tests/VBBSManager.Tests.csproj       tests/VBBSManager.Tests/

RUN dotnet restore

COPY . .

RUN dotnet publish src/VBBSManager.Api/VBBSManager.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN mkdir -p logs
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "VBBSManager.Api.dll"]
```

**Instrução por instrução:**

| Instrução | O que faz |
|---|---|
| `FROM` | Define a imagem base. Tudo começa de uma imagem existente. |
| `WORKDIR` | Define o diretório de trabalho dentro do container. Como um `cd`. |
| `COPY` | Copia arquivos do host para dentro da imagem. |
| `RUN` | Executa um comando durante o build (não em runtime). |
| `EXPOSE` | Documenta qual porta a aplicação usa. Não abre a porta sozinho — isso é feito no Compose. |
| `ENTRYPOINT` | Define o comando que roda quando o container é iniciado. |
| `AS nome` | Nomeia um estágio para uso no multi-stage build. |

### Dockerfile do Frontend (Angular + Nginx)

```dockerfile
# Arquivo: WEB/Dockerfile

FROM node:22-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.27-alpine AS final
COPY --from=build /app/dist/music-manager-web/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

**Observação importante:** `npm ci` (em vez de `npm install`) usa exatamente as versões do `package-lock.json`, garantindo builds reproduzíveis — um princípio fundamental para ambientes Docker.

---

## 4. Multi-stage build — por que usamos

Este é um dos conceitos mais importantes desta configuração. Observe que ambos os Dockerfiles têm **dois `FROM`** — isso é um multi-stage build.

### O problema que resolve

Para compilar código .NET, você precisa do **SDK** (compilador, ferramentas de build, ~700MB). Mas para *executar* o código compilado, você só precisa do **runtime** (~200MB).

Se construíssemos a imagem em um único estágio com o SDK, nossa imagem final teria 700MB. Com multi-stage, a imagem final tem apenas ~200MB.

### Como funciona

```
┌─────────────────────────────────────┐
│ Estágio 1: "build"  (SDK ~700MB)    │
│  - Copia .csproj                    │
│  - dotnet restore (baixa pacotes)   │
│  - Copia código-fonte               │
│  - dotnet publish (compila)         │
│  - Resultado: /app/publish/*.dll    │
└──────────────────┬──────────────────┘
                   │  COPY --from=build
                   ▼  (só os artefatos compilados)
┌─────────────────────────────────────┐
│ Estágio 2: "final" (Runtime ~200MB) │
│  - Copia apenas os .dll compilados  │
│  - Imagem final: ~200MB             │
│  - Sem código-fonte                 │
│  - Sem SDK                          │
│  - Sem ferramentas de build         │
└─────────────────────────────────────┘
```

O estágio "build" existe apenas durante a construção da imagem. A imagem final que vai para produção é **somente** o estágio "final". O código-fonte nunca entra na imagem de produção.

O mesmo princípio se aplica ao frontend Angular:
- Estágio 1: Node.js com ~500MB de `node_modules` compila o Angular
- Estágio 2: nginx apenas com os arquivos JS/CSS estáticos gerados (~5MB de arquivos)

### Cache de camadas

O Docker é inteligente com cache. Observe no Dockerfile da API:

```dockerfile
# Copiado PRIMEIRO (só os .csproj)
COPY src/VBBSManager.Api/VBBSManager.Api.csproj src/VBBSManager.Api/
RUN dotnet restore  ← essa camada é cacheada

# Copiado DEPOIS (o código-fonte)
COPY . .
RUN dotnet publish
```

Por que copiar o `.csproj` separado do código? Porque `dotnet restore` (baixar pacotes NuGet) é lento. Se você mudar apenas um arquivo `.cs`, o Docker percebe que os `.csproj` não mudaram e **reutiliza a camada de restore do cache**, pulando o download dos pacotes. O build seguinte fica muito mais rápido.

**Regra geral:** coloque o que muda com menos frequência no topo do Dockerfile, e o que muda com mais frequência (código-fonte) no final.

---

## 5. Docker Compose — orquestrando múltiplos containers

Docker Compose é uma ferramenta que permite definir e gerenciar **múltiplos containers que trabalham juntos** em um único arquivo YAML.

Sem Compose, para subir este sistema você precisaria:

```bash
# Sem Compose — verboso e sujeito a erro
docker network create vbbs_network
docker volume create postgres_data
docker run -d --name vbbs_postgres \
  --network vbbs_network \
  -e POSTGRES_DB=vbbs_manager_dev \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=... \
  -v postgres_data:/var/lib/postgresql/data \
  -p 5432:5432 \
  postgres:16-alpine

docker run -d --name vbbs_api \
  --network vbbs_network \
  -e ConnectionStrings__Postgres=... \
  -p 5001:8080 \
  api-api:latest

# ... e assim por diante para web e pgAdmin
```

Com Compose, tudo isso vira um `docker compose up`.

### Anatomia do `docker-compose.yml` base

```yaml
services:           # lista de serviços (containers)

  postgres:         # nome do serviço (também é o hostname na rede interna)
    image: postgres:16-alpine    # imagem a usar (do registry)
    environment:                 # variáveis de ambiente passadas ao container
      POSTGRES_DB: ${POSTGRES_DB}  # valor lido do arquivo .env
    volumes:
      - postgres_data:/var/lib/postgresql/data  # volume persistente
    healthcheck:    # Docker verifica se o serviço está saudável antes de continuar
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - vbbs_network

  api:
    build:          # em vez de usar imagem pronta, construir do Dockerfile
      context: .    # diretório onde está o Dockerfile
      dockerfile: Dockerfile
    depends_on:
      postgres:
        condition: service_healthy  # só inicia quando postgres estiver saudável

networks:
  vbbs_network:     # rede virtual compartilhada entre os containers
    driver: bridge

volumes:
  postgres_data:    # volume nomeado gerenciado pelo Docker
```

---

## 6. Arquitetura desta solução

Este projeto usa o padrão **base + override** do Docker Compose, com três arquivos separados:

```
docker-compose.yml           ← base (compartilhado entre local e produção)
docker-compose.override.yml  ← adições para LOCAL (carregado automaticamente)
docker-compose.prod.yml      ← adições para PRODUÇÃO (carregado explicitamente)
```

### Por que três arquivos?

Porque local e produção são ambientes diferentes com necessidades diferentes:

| Necessidade | Local | Produção |
|---|---|---|
| Banco com porta exposta (5432) | Sim — acesso pelo DBeaver/DataGrip | Não — segurança |
| pgAdmin | Sim — interface visual para o banco | Não — overhead desnecessário |
| API com porta exposta (5001) | Sim — testar endpoints diretamente | Não — só acessível via nginx |
| restart: always | Não | Sim — se o container cair, reinicia |
| Frontend na porta 80 | Não — usamos 4200 para não conflitar | Sim — porta padrão HTTP |
| ASPNETCORE_ENVIRONMENT | Development | Production |

### docker-compose.yml (base)

Define os serviços, sem expor portas. É o que ambos os ambientes têm em comum.

```yaml
services:
  postgres:   # serviço de banco — sem port binding
  api:        # backend .NET — sem port binding
  web:        # frontend nginx — sem port binding
networks:
  vbbs_network:
volumes:
  postgres_data:
```

### docker-compose.override.yml (local)

**Carregado automaticamente** pelo Compose quando você roda `docker compose up` sem especificar arquivo. O Docker Compose funde o `docker-compose.yml` com o `docker-compose.override.yml` automaticamente.

Adiciona ao base:
- Port bindings para acesso pelo host
- Serviço pgAdmin
- `ASPNETCORE_ENVIRONMENT=Development`

```yaml
services:
  postgres:
    ports:
      - "5432:5432"   # host:container

  pgadmin:            # serviço adicional, só existe no override
    image: dpage/pgadmin4:latest
    ports:
      - "5050:80"

  api:
    ports:
      - "5001:8080"

  web:
    ports:
      - "4200:80"
```

### docker-compose.prod.yml (produção)

Carregado **explicitamente** junto com o base. Nunca é carregado sozinho.

```bash
# Produção
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

Adiciona ao base:
- `restart: always` em todos os serviços críticos
- Port binding apenas do `web` (porta 80)
- `ASPNETCORE_ENVIRONMENT=Production`

### Como os arquivos são fundidos (merge)

Quando o Docker Compose carrega múltiplos arquivos, ele **funde** as definições. Campos escalares (string, número) são substituídos pelo valor do arquivo mais específico. Listas são **somadas** (não substituídas).

```yaml
# base: postgres sem portas
postgres:
  image: postgres:16-alpine

# override: adiciona portas
postgres:
  ports:
    - "5432:5432"

# resultado fundido
postgres:
  image: postgres:16-alpine
  ports:
    - "5432:5432"
```

---

## 7. Redes internas e comunicação entre serviços

### Como containers se comunicam

Todos os serviços definidos no Compose estão na mesma rede virtual (`vbbs_network`). Dentro dessa rede, **cada serviço pode ser acessado pelo seu nome** definido no Compose.

```
┌──────────────────────────────────────────────────┐
│  Rede Docker: vbbs_network                       │
│                                                  │
│  ┌──────────┐    ┌──────────┐    ┌───────────┐  │
│  │ postgres │    │   api    │    │    web    │  │
│  │ :5432    │◄───│ :8080    │◄───│ nginx:80  │  │
│  └──────────┘    └──────────┘    └───────────┘  │
│                                                  │
└──────────────────────────────────────────────────┘
         ▲                  ▲               ▲
     (não exposto)     (não exposto)   porta 80
                                      (ou 4200 local)
                                      acessível
                                      pelo host
```

### Hostname interno vs. porta no host

**Dentro da rede Docker:**
- A API acessa o banco em: `Host=postgres;Port=5432` (nome do serviço + porta interna)
- O nginx acessa a API em: `http://api:8080` (nome do serviço + porta interna)

**No seu computador (host):**
- Você acessa a API em: `http://localhost:5001` (localhost + porta mapeada no override)
- Você acessa o banco em: `localhost:5432`
- Você acessa o frontend em: `http://localhost:4200`

Isso é por que a string de conexão no `docker-compose.yml` usa `Host=postgres` (não `localhost`):

```yaml
# CORRETO — dentro do container, o banco está no hostname "postgres"
ConnectionStrings__Postgres=Host=postgres;Port=5432;...

# ERRADO — "localhost" dentro do container aponta para o próprio container, não para o banco
ConnectionStrings__Postgres=Host=localhost;Port=5432;...
```

### Port binding: `HOST:CONTAINER`

A notação de portas no Compose é sempre `PORTA_NO_HOST:PORTA_NO_CONTAINER`:

```yaml
ports:
  - "5001:8080"   # localhost:5001 → container:8080
  - "4200:80"     # localhost:4200 → container:80
  - "5432:5432"   # localhost:5432 → container:5432
```

O container continua usando sua porta interna (8080, 80). O mapeamento é como um redirecionamento de porta no host para dentro do container.

### Nginx como reverse proxy

O container `web` (nginx) faz mais do que servir arquivos estáticos — ele também funciona como **reverse proxy** para a API.

```
Navegador                    Container web (nginx)       Container api
   │                               │                          │
   │  GET /api/auth/login          │                          │
   │─────────────────────────────►│                          │
   │                               │  proxy_pass http://api:8080
   │                               │─────────────────────────►│
   │                               │                          │
   │                               │  ← 200 OK               │
   │  ← 200 OK                     │                          │
```

**Por que isso é importante:**

No ambiente de produção, o Angular e a API ficam na mesma origem (mesmo domínio/porta). O Angular faz requests para `/api/...` (caminho relativo), o nginx intercepta e encaminha para o container da API internamente. O navegador nunca sabe que existe um serviço separado.

Isso elimina problemas de **CORS** (Cross-Origin Resource Sharing) — que aconteceria se o frontend no domínio `app.vbbs.com.br` tentasse chamar `api.vbbs.com.br`.

A configuração no `nginx.conf`:

```nginx
# Intercepta qualquer request que comece com /api/
location /api/ {
    proxy_pass http://api:8080;  # encaminha para o container "api" na porta 8080
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
}

# Qualquer outra rota serve o index.html do Angular (SPA routing)
location / {
    try_files $uri $uri/ /index.html;
}
```

**`try_files $uri $uri/ /index.html`** é a configuração essencial para SPAs (Single Page Applications). Como o Angular gerencia as rotas no browser (`/dashboard`, `/financial`, etc.), essas rotas não existem como arquivos físicos no servidor. Sem essa linha, um F5 na página `/dashboard` retornaria 404. Com ela, qualquer rota desconhecida serve o `index.html` e o Angular assume o controle do roteamento.

---

## 8. Volumes e persistência de dados

### Por que volumes existem

Containers são **efêmeros**: quando você remove um container (`docker compose down`), tudo dentro dele some. Se o PostgreSQL guardasse os dados dentro do container, você perderia o banco inteiro ao recriar os containers.

Volumes são **diretórios persistentes** que vivem fora dos containers, gerenciados pelo Docker. Eles sobrevivem à remoção e recriação de containers.

### Como está configurado

```yaml
# No docker-compose.yml
postgres:
  volumes:
    - postgres_data:/var/lib/postgresql/data
    # VOLUME_NOMEADO : CAMINHO_DENTRO_DO_CONTAINER

volumes:
  postgres_data:   # declaração do volume nomeado
```

`/var/lib/postgresql/data` é onde o PostgreSQL armazena todos os dados dentro do container. Ao mapear esse caminho para o volume `postgres_data`, os dados ficam salvos no host mesmo que o container seja recriado.

### Tipos de volumes

**Volume nomeado** (o que usamos):
```yaml
volumes:
  - postgres_data:/var/lib/postgresql/data
```
O Docker gerencia onde os arquivos ficam no host (geralmente em `/var/lib/docker/volumes/` no Linux, ou em uma VM interna no Mac). Você não precisa saber o caminho físico — o Docker cuida disso.

**Bind mount** (alternativa, mais usada para desenvolvimento):
```yaml
volumes:
  - ./src:/app/src   # mapeia uma pasta do host para o container
```
Útil para desenvolvimento sem Docker: você edita o arquivo no host e o container vê imediatamente a alteração. Não usamos isso aqui porque optamos por builds reproduzíveis.

### Comportamento do `docker compose down`

```bash
docker compose down          # para e remove containers. VOLUMES são preservados.
docker compose down -v       # para, remove containers E remove volumes (APAGA OS DADOS).
```

**Cuidado com `-v` em produção** — ele apaga todos os dados do banco.

---

## 9. Variáveis de ambiente e segredos

### Por que não hardcodar credenciais

Se você escrever a senha do banco direto no `docker-compose.yml` e commitar, essa senha vai para o histórico do Git para sempre. Mesmo que você corrija depois, ela continua no histórico.

### O padrão `.env`

O Docker Compose lê automaticamente um arquivo `.env` no mesmo diretório do `docker-compose.yml` e substitui as variáveis `${VAR}`:

```bash
# Arquivo .env (nunca commitado — está no .gitignore)
POSTGRES_PASSWORD=postgres_local_dev
JWT_SECRET=dev-secret-key-change-in-production-min-32chars!!
```

```yaml
# docker-compose.yml — usa as variáveis sem expor os valores
environment:
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}   # substituído em runtime
  Jwt__Secret: ${JWT_SECRET}
```

### Hierarquia de configuração no .NET

A API recebe a configuração em ordem de prioridade (mais alta sobrescreve mais baixa):

```
1. appsettings.json                    (menor prioridade)
2. appsettings.{Environment}.json      (ex: appsettings.Development.json)
3. Variáveis de ambiente               (maior prioridade)
```

Quando o Compose injeta `ConnectionStrings__Postgres=Host=postgres;...` como variável de ambiente, isso **sobrescreve** o valor do `appsettings.json`. O `__` (duplo underscore) é a notação do .NET para referenciar propriedades aninhadas:

```
ConnectionStrings__Postgres  →  appsettings.json: { "ConnectionStrings": { "Postgres": "..." } }
Jwt__Secret                  →  appsettings.json: { "Jwt": { "Secret": "..." } }
```

### .env.example

O arquivo `.env.example` é commitado no repositório. Ele serve como **template documentado** de quais variáveis são necessárias, sem expor os valores reais. Quando alguém clona o projeto:

```bash
cp .env.example .env
# editar .env com os valores reais
docker compose up -d
```

---

## 10. O fluxo completo — do código ao container

```
Código-fonte (.cs, .ts)
         │
         │  docker compose build
         ▼
┌─────────────────────┐
│   Dockerfile        │
│   (receita)         │
│                     │
│  1. FROM (base)     │
│  2. COPY .csproj    │
│  3. dotnet restore  │
│  4. COPY código     │
│  5. dotnet publish  │  ← estágio "build" (SDK)
│  6. FROM runtime    │
│  7. COPY artefatos  │  ← estágio "final" (runtime only)
└─────────┬───────────┘
          │
          ▼
      Imagem Docker
   (arquivo empacotado)
          │
          │  docker compose up
          ▼
      Container
  (processo rodando,
   isolado, com rede
   e volumes)
```

---

## 11. Como funciona no ambiente local

### Estrutura em execução

```
Seu Mac (host)
│
├── localhost:4200  ──►  Container vbbs_web  (nginx)
│                              │
│                              │  nginx proxy_pass http://api:8080
│                              ▼
├── localhost:5001  ──►  Container vbbs_api  (ASP.NET Core)
│                              │
│                              │  Host=postgres;Port=5432
│                              ▼
├── localhost:5432  ──►  Container vbbs_postgres  (PostgreSQL)
│
└── localhost:5050  ──►  Container vbbs_pgadmin  (pgAdmin)
```

### Comando para subir

```bash
cd API/

# Sobe todos os containers (carrega docker-compose.yml + docker-compose.override.yml)
docker compose up -d

# -d = detached (roda em background, terminal fica livre)
```

### O que o `docker-compose.override.yml` adiciona

Sem o override, nenhuma porta seria acessível pelo host — os containers se enxergam internamente, mas você não conseguiria abrir `localhost:4200` no navegador. O override é o que abre essas "janelas" do host para dentro da rede Docker.

### Fluxo de uma requisição local

1. Você abre `http://localhost:4200` no navegador
2. A requisição chega ao container `vbbs_web` (nginx) na porta 80
3. Nginx serve o `index.html` do Angular
4. O Angular carrega no browser e exibe a tela de login
5. Você faz login — Angular envia `POST /api/auth/login`
6. Nginx intercepta (começa com `/api/`) e encaminha para `http://api:8080/api/auth/login`
7. A API processa, consulta `postgres:5432`, retorna o JWT
8. Nginx devolve a resposta ao browser

**Nota importante:** `apiUrl: '/api'` no `environment.prod.ts` é um caminho relativo. Quando o Angular está sendo servido pelo nginx (`localhost:4200`), `/api/auth/login` resolve para `localhost:4200/api/auth/login` — que cai no nginx, que faz o proxy. Isso funciona identicamente em produção com o domínio real.

---

## 12. Como funciona em produção

### Diferenças em relação ao local

Em produção (VPS Hostinger):
- Nenhuma porta exposta diretamente, exceto a porta 80 do nginx (via `docker-compose.prod.yml`)
- Banco e API são **invisíveis** externamente — só acessíveis dentro da rede Docker
- `restart: always` garante que os serviços reiniciam se o container cair ou o servidor reiniciar

### Setup inicial na VPS

```bash
# 1. Clonar os dois repositórios lado a lado
mkdir -p /opt/vbbs
cd /opt/vbbs
git clone https://github.com/ViniciusBernucci/VBBS-Manager-API.git API
git clone https://github.com/ViniciusBernucci/VBBS-Manager-WEB.git WEB

# 2. Criar o .env de produção com valores reais e seguros
cd API
cp .env.example .env
nano .env
# Alterar:
#   POSTGRES_PASSWORD=<senha_forte_aqui>
#   JWT_SECRET=<string_aleatória_longa_aqui>  → openssl rand -base64 48
#   ASPNETCORE_ENVIRONMENT=Production
#   (remover PGADMIN_EMAIL e PGADMIN_PASSWORD — não usados em prod)

# 3. Subir com o compose de produção
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

### Deploy de nova versão

```bash
cd /opt/vbbs

# Atualizar o código
git -C API pull
git -C WEB pull

# Reconstruir as imagens e recriar os containers atualizados
# (--build força rebuild; containers com imagem antiga são substituídos)
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build
```

O `--build` faz o Docker reconstruir as imagens. Containers que não tiveram mudança de imagem permanecem rodando sem interrupção. O PostgreSQL (que usa imagem do registry, não build local) não é afetado.

### Estrutura em produção

```
Internet
    │
    ▼ porta 80
Container vbbs_web (nginx)
    │
    │  rede interna vbbs_network
    ├──► http://api:8080  →  Container vbbs_api
    │                             │
    │                             ▼
    │                    Container vbbs_postgres
    │
    └─ (nenhuma outra porta acessível externamente)
```

### HTTPS em produção

O `docker-compose.prod.yml` expõe apenas a porta 80. Para HTTPS, a recomendação é instalar um **Nginx no host** (fora do Docker) como reverse proxy com certificado SSL (Let's Encrypt), que encaminha as requisições HTTPS para o container na porta 80. Isso será configurado na Fase 3.

Alternativa: adicionar Traefik como serviço no Compose para gerenciar SSL automaticamente.

---

## 13. Comandos do dia a dia

### Gerenciar containers

```bash
# Subir tudo em background
docker compose up -d

# Subir e ver os logs em tempo real (Ctrl+C para sair sem parar os containers)
docker compose up

# Parar os containers (não remove, não perde dados)
docker compose stop

# Parar e remover containers (volumes são preservados)
docker compose down

# Parar, remover containers E apagar volumes (APAGA OS DADOS DO BANCO)
docker compose down -v

# Ver status de todos os containers
docker compose ps

# Reiniciar um serviço específico
docker compose restart api
```

### Ver logs

```bash
# Logs de todos os serviços
docker compose logs

# Logs de um serviço específico
docker compose logs api
docker compose logs web

# Logs em tempo real (follow)
docker compose logs -f api

# Últimas 50 linhas
docker compose logs --tail=50 api
```

### Build e atualização

```bash
# Reconstruir todas as imagens sem usar cache (build limpo)
docker compose build --no-cache

# Reconstruir apenas a API
docker compose build api

# Reconstruir e recriar os containers
docker compose up -d --build
```

### Entrar dentro de um container

```bash
# Abrir um shell interativo no container da API
docker compose exec api sh

# Abrir bash no container do postgres
docker compose exec postgres bash

# Executar um comando no container sem abrir shell interativo
docker compose exec postgres psql -U postgres -d vbbs_manager_dev
```

### Inspecionar recursos Docker

```bash
# Listar todas as imagens locais
docker images

# Listar todos os containers (incluindo parados)
docker ps -a

# Ver os volumes criados
docker volume ls

# Inspecionar um volume (mostra onde os dados ficam no host)
docker volume inspect api_postgres_data

# Remover imagens e containers que não estão em uso (libera espaço)
docker system prune

# Remover tudo incluindo volumes não usados (CUIDADO)
docker system prune -a --volumes
```

### Conectar ao banco de dados

```bash
# Via psql dentro do container (sem expor porta)
docker compose exec postgres psql -U postgres -d vbbs_manager_dev

# Via ferramenta externa (DBeaver, DataGrip, TablePlus)
# Host: localhost
# Port: 5432
# Database: vbbs_manager_dev
# User: postgres
# Password: (valor do POSTGRES_PASSWORD no .env)
```

---

## 14. O que acontece quando você executa `docker compose up`

Passo a passo detalhado do que o Docker Compose faz:

### 1. Leitura dos arquivos

```bash
docker compose up -d
# Compose lê: docker-compose.yml + docker-compose.override.yml (automático)
# Funde as definições
# Lê o arquivo .env para substituir as variáveis ${VAR}
```

### 2. Verificação de imagens

Para cada serviço:
- Se tem `build:` → verifica se a imagem precisa ser (re)construída
- Se tem `image:` → verifica se a imagem existe localmente; se não, baixa do registry

### 3. Criação da rede

Se `vbbs_network` não existe, cria. Se já existe, reutiliza.

### 4. Criação dos volumes

Se `postgres_data` não existe, cria. Se já existe, reutiliza (dados preservados).

### 5. Inicialização em ordem de dependência

O Compose respeita `depends_on`:

```
postgres  →  api
          →  pgadmin
postgres  →  web (indiretamente, via api)
```

1. Inicia `postgres`
2. Aguarda o `healthcheck` do postgres passar (`pg_isready`)
3. Inicia `api` e `pgadmin` (dependem de `postgres: healthy`)
4. Inicia `web` (depende de `api`)

### 6. Injeção de variáveis de ambiente

Antes de iniciar cada container, o Compose injeta as variáveis de ambiente definidas em `environment:`, com os valores resolvidos do `.env`.

### 7. Port binding

O Compose configura o mapeamento de portas do host para o container.

### 8. Container em execução

O container executa o comando definido no `ENTRYPOINT` ou `CMD` do Dockerfile:
- API: `dotnet VBBSManager.Api.dll`
- Web: `nginx -g 'daemon off;'`
- Postgres: o entrypoint padrão da imagem oficial

---

## 15. Decisões de arquitetura deste projeto

### Por que 3 arquivos de Compose e não 1?

Alternativa descartada: um único `docker-compose.yml` com condicionais ou profiles.

A razão: clareza. Cada arquivo tem um propósito único e é fácil de entender isoladamente. O desenvolvedor lê `docker-compose.override.yml` e entende imediatamente "isso é o que roda só em dev".

### Por que não usar volumes bind-mount no desenvolvimento?

Em vez de:
```yaml
volumes:
  - ./src:/app   # bind mount do código-fonte
```

Usamos o build completo também em desenvolvimento. A razão: o objetivo desta configuração Docker é garantir que o ambiente local seja o mais parecido possível com produção. Se o build produz um artefato, você está testando o mesmo artefato. Bind mounts para código-fonte são úteis para hot-reload, mas adicionam uma diferença entre local e produção.

Para desenvolvimento sem reconstruir a imagem a cada mudança, use o ambiente sem Docker (dotnet run / ng serve) — que é mais rápido para iterações. O Docker é ativado para testar o sistema completo integrado ou para validar antes de um deploy.

### Por que o nginx está no container do frontend e não separado?

Alternativa: ter um container nginx separado servindo o frontend e fazendo proxy.

A escolha: embutir nginx na imagem do frontend simplifica — o container `web` é autocontido. Adicionar um nginx externo seria útil se precisássemos de um reverse proxy compartilhado por vários serviços (como SSL termination). Isso entra quando configurarmos HTTPS na Fase 3.

### Por que a API não está diretamente exposta em produção?

Toda requisição externa entra pelo nginx (`web`). Isso dá:
- Um único ponto de entrada
- Controle centralizado de headers, logs, rate limiting
- A API nunca fica visível externamente — proteção adicional
- Futuro: fácil adicionar autenticação, caching ou HTTPS no nginx sem tocar na API

---

*Documentação criada em Fase 0 do projeto VBBS Manager.*
