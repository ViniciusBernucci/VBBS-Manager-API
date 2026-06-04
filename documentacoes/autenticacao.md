# Autenticação

JWT de curta duração (15 minutos) + Refresh Token persistido no banco (7 dias).

---

## Por que não JWT stateless puro?

JWT stateless não permite revogar tokens antes de expirar. Em um sistema multi-tenant, isso é um problema: não seria possível bloquear um tenant imediatamente em caso de problema. Com refresh token no banco, qualquer sessão pode ser invalidada a qualquer momento.

---

## Fluxo completo

```
1. POST /api/auth/login
   → valida email + senha no banco
   → gera access token JWT (expira em 15min)
   → gera refresh token (UUID aleatório, expira em 7 dias, salvo no banco)
   → retorna os dois tokens

2. Frontend armazena:
   - access token em memória (nunca em localStorage)
   - refresh token em cookie HttpOnly

3. A cada requisição:
   → frontend envia access token no header Authorization: Bearer <token>
   → TenantMiddleware extrai tenant_id do claim e injeta no contexto

4. Quando access token expira (401):
   → frontend chama POST /api/auth/refresh com o refresh token
   → backend valida: token existe, não está revogado, não expirou
   → revoga o refresh token atual
   → emite novo par (access token + refresh token)
   → retorna ao cliente

5. Logout:
   → backend revoga o refresh token (seta revoked_at)
   → frontend descarta o access token da memória
```

---

## Estrutura do JWT

**Claims do access token:**

| Claim | Valor |
|---|---|
| `sub` | UUID do usuário |
| `tenant_id` | UUID do tenant |
| `name` | Nome do usuário |
| `iss` | `vbbs-manager` |
| `aud` | `vbbs-manager-web` |
| `exp` | Unix timestamp de expiração (15min) |

O `tenant_id` no token é o que o `TenantMiddleware` lê para injetar o contexto de isolamento.

---

## Configuração (appsettings)

```json
"Jwt": {
  "Secret": "${JWT_SECRET}",
  "Issuer": "vbbs-manager",
  "Audience": "vbbs-manager-web",
  "AccessTokenExpirationMinutes": 15,
  "RefreshTokenExpirationDays": 7
}
```

O `JWT_SECRET` nunca entra no repositório. Em dev usa `appsettings.Development.json` com um valor fixo. Em produção vem de variável de ambiente ou secrets do servidor.

---

## Revogação de sessão

Para revogar todas as sessões de um usuário (ex: troca de senha, suspeita de comprometimento):

```sql
UPDATE refresh_tokens
SET revoked_at = now()
WHERE user_id = '<user_id>'
  AND tenant_id = '<tenant_id>'
  AND revoked_at IS NULL;
```

Como o access token tem apenas 15 minutos, após a revogação do refresh token a sessão expira em no máximo 15 minutos sem nenhuma ação adicional.
