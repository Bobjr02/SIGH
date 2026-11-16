import { randomBytes } from 'node:crypto';
import { defineConfig, devices, type PlaywrightTestConfig } from '@playwright/test';
import { baseConnectionString, userSecret, withDatabase } from './e2e/support/e2eDatabase';

// E2E (veja e2e/README.md). Dois alvos:
//   - padrão ("dev"): a API sobe com "dotnet run" e o front com o Vite, contra o SQL Server do docker-compose.yml;
//   - E2E_TARGET=stack: a stack em containers do compose.app.yml + compose.e2e.yml, já de pé
//     (scripts/e2e-stack.sh sobe, roda e desmonta; é o que o CI usa).
// Cada execução usa um banco novo SIGH_E2E_<timestamp>: a API o cria (migrations + seed) e o
// globalTeardown o apaga ao final, mesmo se algum teste falhar.
// O config é avaliado também nos workers: o nome do banco e as senhas vão para process.env uma vez só.
const STACK = process.env.E2E_TARGET === 'stack';
const CI = !!process.env.CI;
if (!process.env.E2E_DB_NAME) {
  const stamp = new Date().toISOString().replace(/\D/g, '').slice(0, 14);
  process.env.E2E_DB_NAME = `SIGH_E2E_${stamp}`;
}
process.env.E2E_ADMIN_PASSWORD ??= userSecret('Seed:AdminPassword');
process.env.E2E_NEW_PASSWORD ??= `E2e#${randomBytes(6).toString('hex')}Aa1`;

const apiConnectionString = STACK ? '' : withDatabase(baseConnectionString(), process.env.E2E_DB_NAME);

// Dev: API (sem reaproveitar uma já de pé) apontando para o banco desta execução + Vite.
const devServers: PlaywrightTestConfig['webServer'] = [
  {
    command: 'dotnet run --project ../backend/src/SIGH.Api --launch-profile http',
    url: 'http://localhost:5000/api/v1/health',
    reuseExistingServer: false,
    timeout: 240_000,
    env: {
      ConnectionStrings__DefaultConnection: apiConnectionString,
      ASPNETCORE_ENVIRONMENT: 'Development',
      // Todos os specs fazem login do mesmo IP em poucos minutos; o padrão (10/min) é para produção.
      // (Na stack, os mesmos valores estão no compose.e2e.yml.)
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
];

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  // No CI também o relatório HTML (publicado como artifact quando falha).
  reporter: CI ? [['list'], ['html', { open: 'never' }]] : [['list']],
  timeout: 90_000,
  globalSetup: './e2e/support/globalSetup.ts',
  globalTeardown: './e2e/support/globalTeardown.ts',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:3000',
    headless: true,
    trace: 'retain-on-failure',
    acceptDownloads: true,
    // Codespace/CI com /dev/shm pequeno: sem isto o Chromium pode cair ("Target crashed").
    launchOptions: { args: ['--disable-dev-shm-usage'] },
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
    // Por último: importa a carga do pacote (desliga por ausência os funcionários sintéticos) e o histórico.
    { name: 'history', testMatch: /history\.spec\.ts/, dependencies: ['app'], use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: STACK ? undefined : devServers,
});
