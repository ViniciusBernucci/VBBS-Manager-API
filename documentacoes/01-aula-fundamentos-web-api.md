# Aula 01 — Fundamentos de Web API (antes de abrir o código)

> **Para quem é esta aula?**
> Para quem está aprendendo C# e .NET **usando este projeto como laboratório**, mas ainda não domina conceitos de HTTP, REST, JSON e autenticação em APIs. Leia este documento **antes** de mergulhar no código.

**Próximo passo na trilha:** [00-trilha-de-aprendizado.md](./00-trilha-de-aprendizado.md) (visão completa) → depois [02-readme.md](./02-readme.md) (item 02) e [03-ambiente-local.md](./03-ambiente-local.md) (item 03)

---

## Sumário

1. [O que é uma Web API?](#1-o-que-é-uma-web-api)
2. [HTTP — a linguagem da web](#2-http--a-linguagem-da-web)
3. [REST — o estilo que seguimos](#3-rest--o-estilo-que-seguimos)
4. [JSON — o formato dos dados](#4-json--o-formato-dos-dados)
5. [Do navegador ao seu Controller](#5-do-navegador-ao-seu-controller)
6. [Swagger — testar a API sem frontend](#6-swagger--testar-a-api-sem-frontend)
7. [Configuração no ASP.NET Core](#7-configuração-no-aspnet-core)
8. [Tipos de autenticação em APIs](#8-tipos-de-autenticação-em-apis)
9. [Webhooks — quando a API externa te chama](#9-webhooks--quando-a-api-externa-te-chama)
10. [Glossário rápido](#10-glossário-rápido)

---

## 1. O que é uma Web API?

Uma **Web API** é um programa que **escuta requisições pela rede** (HTTP) e **responde com dados** — geralmente JSON.

No VBBS Manager, a API é o backend: o frontend Angular (ou o Swagger) envia pedidos; a API processa, consulta o banco ou APIs externas, e devolve a resposta.

```
Frontend (Angular)          API (.NET)              Banco / Hotmart
      │                        │                         │
      │  GET /api/financial/dre│                         │
      │ ──────────────────────►│                         │
      │                        │  SELECT no PostgreSQL   │
      │                        │ ───────────────────────►│
      │                        │◄─────────────────────── │
      │  JSON com o DRE        │                         │
      │ ◄──────────────────────│                         │
```

**Analogia com Laravel:** um Controller Laravel que retorna JSON é exatamente o mesmo papel do Controller ASP.NET Core neste projeto.

**Analogia com Node/Express:** `app.get('/api/...', handler)` ≈ `[HttpGet]` em um Controller C#.

---

## 2. HTTP — a linguagem da web

Toda comunicação com a API usa o protocolo **HTTP**. Uma requisição tem:

| Parte | O que é | Exemplo |
|---|---|---|
| **Método** | A intenção da operação | `GET`, `POST`, `PATCH`, `DELETE` |
| **URL** | Endereço do recurso | `http://localhost:5000/api/auth/login` |
| **Headers** | Metadados | `Authorization: Bearer eyJ...`, `Content-Type: application/json` |
| **Body** | Corpo (opcional) | `{ "email": "...", "password": "..." }` |

A resposta também tem **status code** + **body**:

| Status | Significado | Quando aparece no projeto |
|---|---|---|
| **200 OK** | Sucesso | Login correto, DRE retornado |
| **201 Created** | Recurso criado | Nova transação no fluxo de caixa |
| **400 Bad Request** | Dados inválidos | Email mal formatado no login |
| **401 Unauthorized** | Não autenticado | Token JWT ausente ou expirado |
| **403 Forbidden** | Autenticado, sem permissão | (futuro multi-usuário) |
| **404 Not Found** | Recurso não existe | Alerta com ID inexistente |
| **500 Internal Server Error** | Erro inesperado no servidor | Bug não tratado — `ExceptionMiddleware` captura |

### Métodos HTTP mais usados

| Método | Uso típico | Idempotente?* |
|---|---|---|
| `GET` | Ler dados | Sim |
| `POST` | Criar ou ação (login) | Não |
| `PUT` | Substituir recurso inteiro | Sim |
| `PATCH` | Atualizar parte do recurso | Não |
| `DELETE` | Remover | Sim |

*Idempotente = chamar várias vezes produz o mesmo efeito (ex.: `GET` não altera o banco).

No projeto, a convenção está documentada em [09-endpoints.md](./09-endpoints.md).

---

## 3. REST — o estilo que seguimos

**REST** não é uma tecnologia — é um **conjunto de convenções** para organizar URLs e métodos HTTP.

Princípios que você verá no VBBS Manager:

1. **Recursos no plural** — `/api/alerts`, `/api/financial/dre`
2. **Substantivos, não verbos** — `GET /api/alerts` (não `/api/getAlerts`)
3. **Query params para filtros** — `GET /api/financial/overview?from=2026-06-01&to=2026-06-30`
4. **Corpo JSON para entrada** — `POST /api/auth/login` com `{ email, password }`
5. **Resposta JSON** — quase todos os endpoints retornam JSON

Exemplo real do projeto:

```
POST   /api/auth/login              → autenticar
GET    /api/financial/dre?year=2026&month=6  → ler DRE
PATCH  /api/alerts/{id}/read        → marcar alerta como lido
POST   /api/webhooks/hotmart        → receber evento da Hotmart
```

---

## 4. JSON — o formato dos dados

**JSON** (JavaScript Object Notation) é texto estruturado — o “idioma” padrão de APIs modernas.

```json
{
  "grossRevenue": 12000.00,
  "totalSales": 200,
  "isActive": true,
  "tags": ["hotmart", "pix"],
  "tenant": {
    "id": "00000000-0000-0000-0000-000000000001",
    "name": "VBBS Music"
  }
}
```

### Regras básicas

| Tipo JSON | Tipo C# equivalente |
|---|---|
| `"texto"` | `string` |
| `123` ou `123.45` | `int`, `long`, `decimal` |
| `true` / `false` | `bool` |
| `[1, 2, 3]` | `List<T>` ou array |
| `{ "a": 1 }` | `class`, `record` ou `Dictionary` |
| `null` | `null` (tipos nullable: `string?`) |

### camelCase vs PascalCase

- **JSON da API (HTTP):** geralmente `camelCase` — `"accessToken"`, `"grossRevenue"`
- **Propriedades C#:** `PascalCase` — `AccessToken`, `GrossRevenue`

O ASP.NET Core converte automaticamente na maioria dos casos. Para APIs **externas** (Hotmart), usamos `[JsonPropertyName("access_token")]` quando o JSON usa `snake_case`.

Isso é explicado com código real em [12-integracao-hotmart-vendas.md](./12-integracao-hotmart-vendas.md).

---

## 5. Do navegador ao seu Controller

Antes de ler [04-aula-dotnet-conceitos.md](./04-aula-dotnet-conceitos.md), entenda a **cadeia completa**:

```
1. Cliente HTTP (Angular, Swagger, curl)
       ↓
2. Kestrel — servidor web embutido no .NET (escuta porta 5000)
       ↓
3. Pipeline de Middlewares (ordem importa!)
       ExceptionMiddleware → CORS → Authentication → TenantMiddleware → Authorization
       ↓
4. Roteamento — qual Controller e qual método?
       GET /api/financial/dre → DreController.Get(...)
       ↓
5. Controller — recebe HTTP, chama Service, devolve IActionResult
       ↓
6. Service — lógica de negócio (sem saber que veio de HTTP)
       ↓
7. AppDbContext ou ExternalClient — banco ou API externa
       ↓
8. Resposta sobe de volta: Service → Controller → JSON → Cliente
```

**Regra de ouro do projeto:** Controller **não** acessa banco diretamente. Ele delega ao **Service**.

Detalhes de cada camada: [05-arquitetura.md](./05-arquitetura.md) e [04-aula-dotnet-conceitos.md](./04-aula-dotnet-conceitos.md).

---

## 6. Swagger — testar a API sem frontend

**Swagger** (OpenAPI) gera uma **página web interativa** listando todos os endpoints.

Em desenvolvimento: `http://localhost:5000/swagger`

O que você pode fazer:

1. Ver métodos, URLs e parâmetros
2. Clicar em **Try it out**
3. Enviar requisições reais e ver o JSON de resposta
4. Para endpoints 🔒, clicar em **Authorize** e colar o JWT: `Bearer eyJ...`

**Fluxo típico de teste:**

```
1. POST /api/auth/login  → copiar accessToken
2. Authorize no Swagger  → colar "Bearer {token}"
3. GET /api/financial/dre → testar endpoint protegido
```

Swagger é configurado em `ServiceCollectionExtensions.AddSwagger()` e ativado só em Development no `Program.cs`.

---

## 7. Configuração no ASP.NET Core

Aplicações .NET leem configurações de **várias fontes**, nesta ordem de prioridade (a última vence):

```
appsettings.json
    ↓ sobrescrito por
appsettings.Development.json
    ↓ sobrescrito por
Variáveis de ambiente (incluindo .env via DotNetEnv)
    ↓ sobrescrito por
Argumentos de linha de comando
```

### appsettings.json

Arquivo JSON com configurações **não secretas** — connection string com placeholder, nomes de issuer JWT, etc.

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;..."
  },
  "Jwt": {
    "Issuer": "vbbs-manager",
    "Audience": "vbbs-manager-web"
  }
}
```

### appsettings.Development.json

Sobrescreve valores **só em desenvolvimento** — secrets locais, log level Debug.

### Variáveis de ambiente

Padrão .NET para sobrescrever qualquer chave:

```bash
# Seção aninhada usa __ (dois underscores)
export Jwt__Secret="meu-secret-local"
export ConnectionStrings__Postgres="Host=localhost;..."
```

### Arquivo .env (DotNetEnv)

Segredos locais que **nunca vão pro Git**:

```env
JWT_SECRET=...
HOTMART_CLIENT_ID=...
HOTMART_CLIENT_SECRET=...
```

No `Program.cs`:

```csharp
Env.TraversePath().Load();  // carrega .env antes do builder
```

**Onde ler no código:** `IConfiguration configuration` — injetado ou via `builder.Configuration`:

```csharp
var secret = configuration["Jwt:Secret"];           // de appsettings
var clientId = configuration["HOTMART_CLIENT_ID"];  // de .env
```

**Analogia Laravel:** `appsettings.json` ≈ `config/app.php`; `.env` ≈ `.env` do Laravel; `IConfiguration` ≈ `config()` helper.

Mais detalhes operacionais: [03-ambiente-local.md](./03-ambiente-local.md) e [14-docker.md](./14-docker.md) (seção de variáveis).

---

## 8. Tipos de autenticação em APIs

Este projeto usa **dois contextos diferentes** de autenticação. Confundir os dois é erro comum de iniciante.

### 8.1 Autenticação **da sua API** (usuário do painel)

**Quem se autentica:** você (dono) ou futuros usuários do SaaS.

**Como:** email + senha → JWT (15 min) + Refresh Token (7 dias no banco).

**Onde ler:** [08-autenticacao.md](./08-autenticacao.md) e seção 13 da [04-aula-dotnet-conceitos.md](./04-aula-dotnet-conceitos.md).

```
Frontend → Authorization: Bearer {JWT da sua API} → VBBS Manager valida
```

### 8.2 Autenticação **em APIs externas** (Hotmart, Meta, Brevo)

**Quem se autentica:** **seu servidor** (não o usuário final).

**Como varia por provedor:**

| Tipo | Provedor | Como funciona |
|---|---|---|
| **OAuth 2.0 Client Credentials** | Hotmart | `client_id` + `client_secret` → `access_token` temporário |
| **OAuth 2.0 Authorization Code** | Meta Ads | Usuário autoriza no browser → token de longa duração |
| **API Key** | Brevo, Evolution | Chave fixa no header `api-key` |
| **HMAC / Assinatura** | Webhooks Hotmart | Hotmart assina o body; você valida |

```
VBBS Manager → Authorization: Bearer {token da Hotmart} → API Hotmart
```

**Onde ler:** [11-clients-externos.md](./11-clients-externos.md) e [12-integracao-hotmart-vendas.md](./12-integracao-hotmart-vendas.md).

### Resumo visual

```
┌─────────────────────────────────────────────────────────────┐
│  JWT (sua API)                                              │
│  "Quem está logado no painel VBBS?"                         │
│  Header: Authorization: Bearer {jwt emitido pelo Login}     │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  OAuth / API Key (API externa)                              │
│  "O servidor VBBS tem permissão de falar com a Hotmart?"    │
│  Obtido via client_id/secret ou API key — não é o JWT do    │
│  usuário do painel                                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 9. Webhooks — quando a API externa te chama

Normalmente **você chama** a API externa (`GET /sales/history`).

Com **webhook**, a direção **inverte**: a Hotmart (ou Brevo) **envia POST** para o seu servidor quando algo acontece (compra confirmada, reembolso, email aberto).

```
Hotmart  ──POST /api/webhooks/hotmart──►  VBBS Manager
         body: { evento da compra }
         header: X-Hotmart-Signature (validação)
```

No projeto:

- `HotmartWebhookController` — recebe o POST
- `HotmartWebhookService` — valida assinatura, identifica tenant, persiste evento

Endpoints de webhook: [09-endpoints.md](./09-endpoints.md).

---

## 10. Glossário rápido

| Termo | Significado simples |
|---|---|
| **API** | Interface para programas conversarem via rede |
| **Endpoint** | URL + método HTTP específicos (`GET /api/alerts`) |
| **DTO** | Objeto só para transportar dados (Request/Response) |
| **Controller** | Classe que recebe HTTP e devolve resposta |
| **Service** | Classe com lógica de negócio |
| **DI** | Injeção de Dependência — framework cria e injeta objetos |
| **Middleware** | Código que roda em toda requisição (auth, tenant, erros) |
| **ORM** | Mapeia classes C# ↔ tabelas SQL (EF Core) |
| **Migration** | Arquivo versionado que altera o schema do banco |
| **Tenant** | Cliente/empresa isolada no sistema multi-tenant |
| **Typed Client** | `HttpClient` configurado e injetado por interface |
| **Polly** | Biblioteca de retry quando API externa falha |
| **Hangfire** | Agendador de tarefas em background (jobs) |

---

## Checklist — você está pronto para o item 03 da trilha?

- [ ] Sei a diferença entre `GET` e `POST`
- [ ] Sei o que é JSON e status 200 vs 401 vs 500
- [ ] Entendo que Controller → Service → Banco/Client
- [ ] Sei onde ficam secrets (`.env`, não no Git)
- [ ] Sei diferenciar JWT do painel vs token OAuth da Hotmart

Se marcou tudo, siga para [03-ambiente-local.md](./03-ambiente-local.md) e suba o projeto na sua máquina.

---

*Aula 01 — Fundamentos Web API. Parte da [Trilha de Aprendizado](./00-trilha-de-aprendizado.md).*
