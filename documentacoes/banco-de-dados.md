# Banco de Dados

PostgreSQL 16 gerenciado pelo Entity Framework Core com Npgsql.

---

## Entidades e tabelas

### Tenants

Unidade de isolamento do sistema. Todo dado de negócio pertence a um Tenant.

| Coluna | Tipo | Descrição |
|---|---|---|
| id | uuid | PK |
| name | text | Nome da empresa/tenant |
| slug | text | Identificador único em URL (ex: `music-school`) |
| is_active | bool | Tenant ativo ou suspenso |
| created_at | timestamptz | |

---

### Users

Usuários de acesso ao sistema, sempre vinculados a um Tenant.

| Coluna | Tipo | Descrição |
|---|---|---|
| id | uuid | PK |
| tenant_id | uuid | FK → Tenants |
| email | text | Único por tenant |
| password_hash | text | BCrypt |
| name | text | |
| is_active | bool | |
| created_at | timestamptz | |

Índice único: `(tenant_id, email)`.

---

### RefreshTokens

Tokens de renovação de sessão persistidos para permitir revogação imediata.

| Coluna | Tipo | Descrição |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → Users |
| tenant_id | uuid | Desnormalizado para facilitar queries de revogação por tenant |
| token | text | UUID aleatório gerado no momento do login |
| expires_at | timestamptz | `CreatedAt + 7 dias` |
| created_at | timestamptz | |
| revoked_at | timestamptz | `null` se ainda válido |

Um token está ativo se `revoked_at IS NULL` e `expires_at > now()`.

---

### Integrations

Credenciais de APIs externas por tenant, armazenadas criptografadas.

| Coluna | Tipo | Descrição |
|---|---|---|
| id | uuid | PK |
| tenant_id | uuid | FK → Tenants |
| provider | int | Enum: Hotmart, MetaAds, Brevo, EvolutionApi, ClaudeAi |
| credentials_encrypted | text | JSON criptografado com chave derivada do tenant |
| is_active | bool | |
| last_sync_at | timestamptz | Última sincronização bem-sucedida |
| created_at | timestamptz | |
| updated_at | timestamptz | |

---

### Alerts

Alertas gerados automaticamente pelos jobs de sincronização.

| Coluna | Tipo | Descrição |
|---|---|---|
| id | uuid | PK |
| tenant_id | uuid | FK → Tenants |
| type | int | Enum: CpaHigh, RoasLow, RevenueProjectionLow, CtrlLow, CartAbandonment |
| severity | int | Enum: Info, Warning, Critical |
| title | text | Título curto para exibição |
| message | text | Descrição detalhada |
| is_read | bool | |
| is_resolved | bool | |
| resolved_at | timestamptz | |
| metadata | jsonb | Dados adicionais do alerta (ex: nome do criativo, valor do CPA) |
| created_at | timestamptz | |
| updated_at | timestamptz | |

---

## Princípio de isolamento

Toda entidade de negócio tem `tenant_id` — sem exceção.

Queries no banco **sempre** incluem `WHERE tenant_id = @tenantId` para garantir que um tenant nunca acessa dados de outro.

Na **Fase 5** (SaaS), esse isolamento será reforçado com Row Level Security no PostgreSQL:

```sql
ALTER TABLE alerts ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON alerts
  USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
```

Isso cria uma segunda camada de proteção no banco, independente da aplicação.

---

## Migrations

Geradas pelo EF Core CLI. Para criar uma nova migration após alterar uma entidade:

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/VBBSManager.Infrastructure \
  --startup-project src/VBBSManager.Api
```

Para aplicar no banco:

```bash
dotnet ef database update \
  --project src/VBBSManager.Infrastructure \
  --startup-project src/VBBSManager.Api
```

Nunca editar manualmente arquivos de migration já aplicados em produção.
