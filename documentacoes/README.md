# Documentação — VBBS Manager API

Índice de toda a documentação técnica do projeto.

---

## Documentos disponíveis

| Documento | Descrição |
|---|---|
| [arquitetura.md](./arquitetura.md) | Visão geral da arquitetura, projetos e decisões de design |
| [estrutura-de-pastas.md](./estrutura-de-pastas.md) | Mapa completo de arquivos e responsabilidade de cada um |
| [endpoints.md](./endpoints.md) | Referência de todos os endpoints da API |
| [banco-de-dados.md](./banco-de-dados.md) | Entidades, relações e decisões do schema |
| [autenticacao.md](./autenticacao.md) | Fluxo JWT + Refresh Token |
| [jobs.md](./jobs.md) | Jobs Hangfire: scheduler, retry e monitoramento |
| [clients-externos.md](./clients-externos.md) | Clientes de API externa e padrão de integração |
| [ambiente-local.md](./ambiente-local.md) | Como subir o ambiente de desenvolvimento |

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
