# Endpoints da API

Base URL em desenvolvimento: `http://localhost:5000`

Todos os endpoints marcados com 🔒 exigem o header:
```
Authorization: Bearer <access_token>
```

---

## Auth

### POST /api/auth/login

Autentica o usuário e retorna o par de tokens.

**Request body:**
```json
{
  "email": "usuario@exemplo.com",
  "password": "senha"
}
```

**Response 200:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "uuid-do-refresh-token",
  "expiresAt": "2026-06-04T13:15:00Z",
  "userName": "Vinicius",
  "tenantId": "uuid-do-tenant"
}
```

**Response 401:** credenciais inválidas

---

### POST /api/auth/refresh

Renova o access token usando o refresh token. Revoga o refresh token atual e emite um novo par.

**Request body:**
```json
{
  "refreshToken": "uuid-do-refresh-token"
}
```

**Response 200:** mesmo formato do login  
**Response 401:** token inválido, expirado ou revogado

---

## Financial 🔒

### GET /api/financial/overview

KPIs financeiros do período selecionado com variação vs. período anterior equivalente.

**Query params:**
| Param | Tipo | Exemplo | Obrigatório |
|---|---|---|---|
| from | DateOnly | `2026-06-01` | sim |
| to | DateOnly | `2026-06-30` | sim |

**Response 200:**
```json
{
  "grossRevenue": 12000.00,
  "netRevenue": 10200.00,
  "adSpend": 9000.00,
  "estimatedMargin": 1200.00,
  "roas": 1.33,
  "cpa": 45.00,
  "averageTicket": 60.00,
  "totalSales": 200,
  "grossRevenueVariation": 0.08,
  "roasVariation": -0.02,
  "cpaVariation": 0.05
}
```

Variações são números decimais: `0.08` = +8%, `-0.02` = -2%.

---

### GET /api/financial/dre

DRE simplificado do mês com evolução semanal e projeção de fechamento.

**Query params:**
| Param | Tipo | Exemplo | Obrigatório |
|---|---|---|---|
| year | int | `2026` | sim |
| month | int | `6` | sim |

**Response 200:**
```json
{
  "grossRevenue": 12000.00,
  "hotmartFee": 1800.00,
  "adSpend": 9000.00,
  "otherExpenses": 0.00,
  "netRevenue": 10200.00,
  "estimatedMargin": 1200.00,
  "marginPercentage": 0.10,
  "monthProjection": 13500.00,
  "weeklyEvolution": [
    { "date": "2026-06-01", "revenue": 3000.00, "adSpend": 2250.00, "margin": 300.00 },
    { "date": "2026-06-08", "revenue": 3200.00, "adSpend": 2400.00, "margin": 320.00 }
  ]
}
```

---

## Creatives 🔒

### GET /api/creatives

Lista de criativos ativos com métricas e semáforo de desempenho.

**Query params:**
| Param | Tipo | Exemplo | Obrigatório |
|---|---|---|---|
| from | DateOnly | `2026-06-01` | sim |
| to | DateOnly | `2026-06-30` | sim |

**Response 200:**
```json
{
  "items": [
    {
      "externalId": "123456789",
      "name": "Criativo_Reaper_v3",
      "spend": 1200.00,
      "cpa": 42.00,
      "ctr": 0.025,
      "conversions": 28,
      "semaphore": "green",
      "date": "2026-06-01"
    }
  ]
}
```

Valores possíveis de `semaphore`: `"green"`, `"yellow"`, `"red"`.

---

## Alerts 🔒

### GET /api/alerts

Lista alertas do tenant com opção de filtrar apenas os não lidos.

**Query params:**
| Param | Tipo | Default | Obrigatório |
|---|---|---|---|
| onlyUnread | bool | `false` | não |

**Response 200:**
```json
{
  "items": [
    {
      "id": "uuid",
      "type": "CpaHigh",
      "severity": "Warning",
      "title": "CPA acima do threshold",
      "message": "Criativo X com CPA R$78 — threshold configurado: R$60",
      "isRead": false,
      "isResolved": false,
      "createdAt": "2026-06-04T10:30:00Z"
    }
  ],
  "totalUnread": 3
}
```

---

### PATCH /api/alerts/{id}/read

Marca um alerta como lido e opcionalmente como resolvido.

**Path params:** `id` — UUID do alerta

**Query params:**
| Param | Tipo | Default | Descrição |
|---|---|---|---|
| resolved | bool | `false` | Se `true`, marca também como resolvido |

**Response 204:** sem body  
**Response 404:** alerta não encontrado ou não pertence ao tenant

---

## Webhooks

> Endpoints de webhook **não usam autenticação JWT**. A validação é feita por assinatura do payload (HMAC) ou IP de origem, dependendo da integração.

### POST /api/webhooks/hotmart

Recebe eventos da Hotmart (compra confirmada, checkout iniciado, reembolso, etc.).

**Header obrigatório:** `X-Hotmart-Signature`  
**Body:** JSON conforme documentação da Hotmart Webhook API  
**Response 200:** evento aceito  
**Response 400:** assinatura inválida ou payload malformado

---

### POST /api/webhooks/brevo

Recebe eventos de email do Brevo (aberto, clicado, bounce, unsubscribe).

**Body:** JSON conforme documentação do Brevo Webhook  
**Response 200:** evento aceito  
**Response 400:** payload malformado
