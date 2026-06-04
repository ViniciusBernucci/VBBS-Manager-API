# Ambiente de Desenvolvimento Local

---

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Git

---

## 1. Clonar o repositório

```bash
git clone <url-do-repo>
cd "VBBS Manager/API"
```

---

## 2. Subir o banco de dados

```bash
docker compose up -d
```

Isso sobe dois containers:

| Container | Porta | Descrição |
|---|---|---|
| `vbbs_postgres` | 5432 | PostgreSQL 16 |
| `vbbs_pgadmin` | 5050 | pgAdmin 4 (UI do banco) |

Para verificar se estão rodando:

```bash
docker compose ps
```

---

## 3. Restaurar pacotes e compilar

```bash
dotnet restore
dotnet build
```

---

## 4. Aplicar migrations

```bash
dotnet ef database update \
  --project src/VBBSManager.Infrastructure \
  --startup-project src/VBBSManager.Api
```

Se não tiver o EF Core CLI instalado:

```bash
dotnet tool install --global dotnet-ef
```

---

## 5. Rodar a API

```bash
dotnet run --project src/VBBSManager.Api
```

A API sobe em `http://localhost:5000`.

---

## URLs disponíveis em dev

| URL | Descrição |
|---|---|
| `http://localhost:5000/swagger` | Swagger UI — documentação interativa dos endpoints |
| `http://localhost:5000/hangfire` | Hangfire Dashboard — monitoramento de jobs |
| `http://localhost:5050` | pgAdmin — interface visual do PostgreSQL |

**pgAdmin — login:** `admin@vbbs.local` / `admin`  
**pgAdmin — conexão ao banco:** host `postgres`, porta `5432`, user `postgres`, senha `postgres`

---

## Variáveis de ambiente

Para dev local, `appsettings.Development.json` já tem valores funcionais sem precisar de variáveis de ambiente.

Para sobrescrever qualquer valor via env var, use o padrão do .NET com `__` como separador de seção:

```bash
export ConnectionStrings__Postgres="Host=localhost;Port=5432;..."
export Jwt__Secret="meu-secret-aqui"
```

---

## Criar uma nova migration

Após alterar qualquer entidade em `VBBSManager.Domain/Entities/`:

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/VBBSManager.Infrastructure \
  --startup-project src/VBBSManager.Api
```

Sempre revisar o arquivo gerado em `src/VBBSManager.Infrastructure/Persistence/Migrations/` antes de aplicar.

---

## Rodar os testes

```bash
dotnet test
```

---

## Parar o ambiente

```bash
docker compose down
```

Para remover também os dados do banco:

```bash
docker compose down -v
```
