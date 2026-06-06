# Documentação — VBBS Manager API

Índice de toda a documentação técnica do projeto.

---

## Comece por aqui

**Se você está aprendendo C# e .NET com este projeto**, siga a trilha numerada:

### [00-trilha-de-aprendizado.md](./00-trilha-de-aprendizado.md)

Ordem didática de leitura (01 → 14), fases de estudo, exercícios práticos e mapa de dependências entre documentos.

**Primeira aula (fundamentos):** [01-aula-fundamentos-web-api.md](./01-aula-fundamentos-web-api.md) — HTTP, REST, JSON, Swagger, configuração e tipos de autenticação.

---

## Documentos por ordem da trilha

| # | Documento | Descrição |
|---|---|---|
| 01 | [01-aula-fundamentos-web-api.md](./01-aula-fundamentos-web-api.md) | **Aula** — HTTP, REST, JSON, Swagger, `.env`, auth, webhooks |
| 02 | [02-readme.md](./02-readme.md) | Visão geral e stack |
| 03 | [03-ambiente-local.md](./03-ambiente-local.md) | Como subir o ambiente de desenvolvimento |
| 04 | [04-aula-dotnet-conceitos.md](./04-aula-dotnet-conceitos.md) | **Aula principal** — C# e .NET aplicados ao código real |
| 05 | [05-arquitetura.md](./05-arquitetura.md) | Visão geral da arquitetura, projetos e decisões de design |
| 06 | [06-estrutura-de-pastas.md](./06-estrutura-de-pastas.md) | Mapa completo de arquivos e responsabilidade de cada um |
| 07 | [07-banco-de-dados.md](./07-banco-de-dados.md) | Entidades, relações e decisões do schema |
| 08 | [08-autenticacao.md](./08-autenticacao.md) | Fluxo JWT + Refresh Token |
| 09 | [09-endpoints.md](./09-endpoints.md) | Referência de todos os endpoints da API |
| 10 | [10-feature-planejamento.md](./10-feature-planejamento.md) | Estudo de caso — feature completa ponta a ponta |
| 11 | [11-clients-externos.md](./11-clients-externos.md) | Clientes de API externa e padrão de integração |
| 12 | [12-integracao-hotmart-vendas.md](./12-integracao-hotmart-vendas.md) | **Aula** — integração Hotmart Sales History v1 |
| 13 | [13-jobs.md](./13-jobs.md) | Jobs Hangfire: scheduler, retry e monitoramento |
| 14 | [14-docker.md](./14-docker.md) | **Aula** — Docker, Compose e deploy |

---

## Stack resumida

```
Backend:         ASP.NET Core (.NET 8) — MVC Controllers
Arquitetura:     Vertical Slice Architecture
ORM:             Entity Framework Core + Npgsql (PostgreSQL)
Jobs:            Hangfire com backend PostgreSQL
Auth:            JWT 15min + Refresh Token persistido no banco
Banco:           PostgreSQL 16
Containers:      Docker + Docker Compose
```
