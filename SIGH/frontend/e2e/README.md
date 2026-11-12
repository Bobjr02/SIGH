# E2E local (Playwright)

Fluxo coberto em `auth-flow.spec.ts`: login do admin → troca obrigatória de senha → lista de
processos → recarregar a página continua logado → logout. Também verifica que o cookie `sigh_rt`
é HttpOnly/Secure/SameSite=Strict com `Path=/api/v1/auth` e que o refresh token não aparece em
corpo de resposta nem em `localStorage`/`sessionStorage`.

Ainda **não roda no CI**: precisa de SQL Server, API e front de pé.

## Pré-requisitos

1. SQL Server do `docker-compose.yml` rodando.
2. User-secrets da API configurados (`ConnectionStrings:DefaultConnection`, `JwtOptions:SecretKey`,
   `Seed:AdminPassword`).
3. Um admin **recém-criado pelo seed** (com `MustChangePassword = true`). Depois de uma execução o
   admin já trocou a senha; para repetir, aponte a API para um banco novo (o seed cria tudo). Exemplo,
   sem apagar nada do banco de dev:

   ```bash
   cd SIGH/backend/src/SIGH.Api
   CS=$(dotnet user-secrets list | sed -n 's/^ConnectionStrings:DefaultConnection = //p')
   export ConnectionStrings__DefaultConnection=$(echo "$CS" | sed -E "s/Database=[^;]*/Database=SIGH_E2E_$(date +%s)/")
   dotnet run --launch-profile http
   ```

4. Navegador do Playwright: `npx playwright install --with-deps chromium`.

## Rodar

```bash
cd SIGH/frontend
export E2E_ADMIN_PASSWORD="$(dotnet user-secrets list --project ../backend/src/SIGH.Api | sed -n 's/^Seed:AdminPassword = //p')"
export E2E_NEW_PASSWORD='<uma senha que atenda à política de complexidade>'
# opcional: E2E_ADMIN_EMAIL (padrão admin@sigh.com.br), E2E_BASE_URL (padrão http://localhost:3000)
npm run e2e
```

O Playwright sobe o Vite (`npm run dev`, com proxy de `/api` para `http://localhost:5000`) se ele
ainda não estiver rodando.
