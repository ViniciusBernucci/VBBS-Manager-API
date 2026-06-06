# Estrutura de Pastas

Mapa completo do repositório com a responsabilidade de cada arquivo.

```
API/
├── VBBSManager.sln                          Solution com os quatro projetos
├── docker-compose.yml                       PostgreSQL 16 + pgAdmin para dev local
├── .gitignore
│
├── documentacoes/                           Esta pasta — documentação técnica
│
├── src/
│   │
│   ├── VBBSManager.Domain/
│   │   ├── Entities/
│   │   │   ├── BaseEntity.cs                Id, TenantId, CreatedAt, UpdatedAt — base para todas as entidades
│   │   │   ├── Tenant.cs                    Empresa/cliente do sistema (unidade de isolamento)
│   │   │   ├── User.cs                      Usuário vinculado a um Tenant
│   │   │   ├── RefreshToken.cs              Token de renovação de sessão, persistido no banco
│   │   │   ├── Integration.cs               Credenciais criptografadas de uma integração por Tenant
│   │   │   └── Alert.cs                     Alertas gerados automaticamente (CPA alto, ROAS baixo, etc.)
│   │   ├── Enums/
│   │   │   ├── AlertEnums.cs                AlertType (CpaHigh, RoasLow…) e AlertSeverity (Info/Warning/Critical)
│   │   │   └── IntegrationProvider.cs       Hotmart, MetaAds, Brevo, EvolutionApi, ClaudeAi
│   │   └── ValueObjects/                    (vazio — adicionar conforme necessidade)
│   │
│   ├── VBBSManager.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs              DbContext principal com todos os DbSets
│   │   │   ├── Configurations/
│   │   │   │   └── UserConfiguration.cs     Mapeamento fluent da entidade User (índice único por tenant+email)
│   │   │   └── Migrations/                  (vazio — gerado pelo EF Core CLI)
│   │   ├── ExternalClients/
│   │   │   ├── Hotmart/
│   │   │   │   └── HotmartClient.cs         Client HTTP para a API Hotmart com retry via Polly
│   │   │   ├── MetaAds/                     (vazio — a implementar na Fase 1)
│   │   │   ├── Brevo/                       (vazio — a implementar na Fase 1)
│   │   │   └── Evolution/                   (vazio — a implementar na Fase 1)
│   │   └── Jobs/
│   │       ├── MetaAdsSyncJob.cs            Job Hangfire: sincroniza métricas de criativos do Meta Ads
│   │       └── HotmartSyncJob.cs            Job Hangfire: sincroniza vendas do Hotmart
│   │
│   └── VBBSManager.Api/
│       ├── Program.cs                       Bootstrap: DI, Serilog, middlewares, Hangfire, Swagger
│       ├── appsettings.json                 Configurações base (connection string com placeholder)
│       ├── appsettings.Development.json     Configurações de dev (postgres local, log debug)
│       │
│       ├── Common/
│       │   ├── Extensions/
│       │   │   └── ServiceCollectionExtensions.cs   Métodos de extensão para DI (AddDatabase, AddJwtAuth, etc.)
│       │   ├── Middleware/
│       │   │   ├── ExceptionMiddleware.cs   Captura exceções não tratadas, loga e retorna 500 padronizado
│       │   │   └── TenantMiddleware.cs      Extrai tenant_id do JWT e injeta em HttpContext.Items
│       │   └── Results/
│       │       └── Result.cs                Result<T> e Result — padrão de retorno de todos os Services
│       │
│       └── Features/
│           │
│           ├── Auth/
│           │   ├── Login/
│           │   │   ├── LoginRequest.cs      { Email, Password }
│           │   │   ├── LoginResponse.cs     { AccessToken, RefreshToken, ExpiresAt, UserName, TenantId }
│           │   │   ├── LoginService.cs      Valida credenciais, gera JWT + RefreshToken
│           │   │   └── LoginController.cs   POST /api/auth/login
│           │   └── RefreshToken/
│           │       ├── RefreshTokenRequest.cs   { RefreshToken }
│           │       ├── RefreshTokenService.cs   Valida token, revoga antigo, emite novo par
│           │       └── RefreshTokenController.cs POST /api/auth/refresh
│           │
│           ├── Financial/
│           │   ├── Overview/
│           │   │   ├── FinancialOverviewResponse.cs  KPIs: receita, CPA, ROAS, margem + variações
│           │   │   ├── FinancialOverviewService.cs   Calcula KPIs do período e variação vs. anterior
│           │   │   └── FinancialOverviewController.cs GET /api/financial/overview?from=&to=
│           │   └── DRE/
│           │       ├── DreResponse.cs       DRE simplificado + evolução semanal + projeção do mês
│           │       ├── DreService.cs        Agrega receitas e gastos, calcula projeção de fechamento
│           │       └── DreController.cs     GET /api/financial/dre?year=&month=
│           │
│           ├── Creatives/
│           │   ├── List/
│           │   │   ├── CreativeResponse.cs  Lista de criativos com métricas e semáforo
│           │   │   ├── CreativesListService.cs  Busca métricas sincronizadas, aplica semáforo
│           │   │   └── CreativesListController.cs GET /api/creatives?from=&to=
│           │   └── Semaphore/               (vazio — lógica de semáforo a extrair aqui se crescer)
│           │
│           ├── Alerts/
│           │   ├── List/
│           │   │   ├── AlertsListResponse.cs    Lista paginada + total de não lidos
│           │   │   ├── AlertsListService.cs     Filtra alertas por tenant e status
│           │   │   └── AlertsListController.cs  GET /api/alerts?onlyUnread=
│           │   └── MarkRead/
│           │       ├── MarkAlertReadService.cs      Marca lido/resolvido garantindo isolamento por tenant
│           │       └── MarkAlertReadController.cs   PATCH /api/alerts/{id}/read?resolved=
│           │
│           ├── Funnel/
│           │   └── Conversions/             (vazio — a implementar na Fase 2)
│           │
│           ├── Integrations/
│           │   └── Credentials/             (vazio — CRUD de credenciais por tenant, Fase 1)
│           │
│           └── Webhooks/
│               ├── Hotmart/
│               │   ├── HotmartWebhookService.cs     Valida assinatura HMAC, identifica tenant, persiste evento
│               │   └── HotmartWebhookController.cs  POST /api/webhooks/hotmart
│               └── Brevo/
│                   ├── BrevoWebhookService.cs        Identifica evento de email, persiste linkado ao lead
│                   └── BrevoWebhookController.cs     POST /api/webhooks/brevo
│
└── tests/
    └── VBBSManager.Tests/
        ├── Features/                        Testes de Services (unitários com NSubstitute)
        └── Infrastructure/                  Testes de Infrastructure (integração com banco real)
```

---

## Convenção de nomenclatura

| Tipo | Padrão | Exemplo |
|---|---|---|
| Pasta de feature | `NomeModulo/NomeAção` | `Financial/Overview` |
| DTO de entrada | `[Ação]Request` | `LoginRequest` |
| DTO de saída | `[Ação]Response` | `LoginResponse` |
| Interface de serviço | `I[Ação]Service` | `ILoginService` |
| Implementação | `[Ação]Service` | `LoginService` |
| Controller | `[Ação]Controller` | `LoginController` |
| Job Hangfire | `[Integração]SyncJob` | `MetaAdsSyncJob` |
| Client externo | `[Integração]Client` | `HotmartClient` |
