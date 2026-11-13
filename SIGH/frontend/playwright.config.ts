import { randomBytes } from 'node:crypto';
import { defineConfig, devices } from '@playwright/test';
import { baseConnectionString, userSecret, withDatabase } from './e2e/support/e2eDatabase';

// E2E local (ainda fora do CI). Veja e2e/README.md.
// Cada execução usa um banco novo SIGH_E2E_<timestamp>: a API o cria (migrations + seed) e o
// globalTeardown o apaga ao final, mesmo se algum teste falhar.
// O config é avaliado também nos workers: o nome do banco e as senhas vão para process.env uma vez só.
if (!process.env.E2E_DB_NAME) {
  const stamp = new Date().toISOString().replace(/\D/g, '').slice(0, 14);
  process.env.E2E_DB_NAME = `SIGH_E2E_${stamp}`;
}
process.env.E2E_ADMIN_PASSWORD ??= userSecret('Seed:AdminPassword');
process.env.E2E_NEW_PASSWORD ??= `E2e#${randomBytes(6).toString('hex')}Aa1`;

const apiConnectionString = withDatabase(baseConnectionString(), process.env.E2E_DB_NAME);

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  timeout: 90_000,
  globalSetup: './e2e/support/globalSetup.ts',
  globalTeardown: './e2e/support/globalTeardown.ts',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:3000',
    headless: true,
    trace: 'retain-on-failure',
    acceptDownloads: true,
  },
  projects: [
    // Primeiro acesso do admin recém-semeado (troca obrigatória de senha).
    { name: 'first-access', testMatch: /auth-flow\.spec\.ts/, use: { ...devices['Desktop Chrome'] } },
    // Importa os funcionários sintéticos que os specs seguintes usam.
    { name: 'employees', testMatch: /employees\.spec\.ts/, dependencies: ['first-access'], use: { ...devices['Desktop Chrome'] } },
    // O resto usa o admin já com a senha nova e os funcionários importados.
    {
      name: 'app',
      testMatch: /(two-tabs|evidence|password-reset|records)\.spec\.ts/,
      dependencies: ['employees'],
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    {
      // API apontando para o banco desta execução (não reaproveita uma API já de pé).
      command: 'dotnet run --project ../backend/src/SIGH.Api --launch-profile http',
      url: 'http://localhost:5000/health',
      reuseExistingServer: false,
      timeout: 240_000,
      env: {
        ConnectionStrings__DefaultConnection: apiConnectionString,
        ASPNETCORE_ENVIRONMENT: 'Development',
        // Todos os specs fazem login do mesmo IP em poucos minutos; o padrão (10/min) é para produção.
        RateLimits__Login__PermitLimit: '100',
        // Perda de acesso: o cache do SecurityStamp (máx. 30 s) fica em 2 s para o e2e não esperar 30 s.
        Auth__SecurityStampCacheSeconds: '2',
      },
      stdout: 'ignore',
      stderr: 'pipe',
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:3000',
      reuseExistingServer: true,
      timeout: 60_000,
    },
  ],
});

