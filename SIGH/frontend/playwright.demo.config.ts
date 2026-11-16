import { randomBytes } from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig, devices, type PlaywrightTestConfig } from '@playwright/test';
import { baseConnectionString, userSecret, withDatabase } from './e2e/support/e2eDatabase';

// Telas com os DADOS DE DEMONSTRAÇÃO num banco descartável SIGH_E2E_<timestamp> (como o e2e):
//   npx playwright test -c playwright.demo.config.ts
// A API (dotnet run) sobe com Seed:DemoData=true (o mesmo seed da stack local) e o banco é apagado no
// final. Assim o administrador tem senha conhecida (troca obrigatória feita pelo setup) sem mexer no
// volume nem no .env.local da stack em contêineres.
//   - no-english.spec: nenhum termo em inglês na tela, com cada perfil;
//   - accessibility.spec: axe-core (WCAG 2 AA, contraste) nos dois temas;
//   - screenshots.spec: capturas em SIGH/docs/telas (SIGH_SCREENSHOTS=1).
// Com E2E_TARGET=stack (scripts/e2e-stack.sh --demo), usa a stack em containers com o seed de
// demonstração (compose.e2e-demo.yml) em vez de "dotnet run" + Vite; nada de user-secrets.
const HERE = path.dirname(fileURLToPath(import.meta.url));
const STACK = process.env.E2E_TARGET === 'stack';

if (!process.env.E2E_DB_NAME) {
  const stamp = new Date().toISOString().replace(/\D/g, '').slice(0, 14);
  process.env.E2E_DB_NAME = `SIGH_E2E_${stamp}`;
}
process.env.E2E_ADMIN_PASSWORD ??= userSecret('Seed:AdminPassword');
process.env.E2E_NEW_PASSWORD ??= `Demo#${randomBytes(6).toString('hex')}Aa1`;
process.env.SIGH_DEMO_PASSWORD ??= `Demo#${randomBytes(6).toString('hex')}Bb2`;

const apiConnectionString = STACK ? '' : withDatabase(baseConnectionString(), process.env.E2E_DB_NAME);

const devServers: PlaywrightTestConfig['webServer'] = [
  {
    command: 'dotnet run --project ../backend/src/SIGH.Api --launch-profile http',
    url: 'http://localhost:5000/api/v1/health',
    reuseExistingServer: false,
    timeout: 300_000,
    env: {
      ConnectionStrings__DefaultConnection: apiConnectionString,
      ASPNETCORE_ENVIRONMENT: 'Development',
      Seed__DemoData: 'true',
      Seed__DemoPassword: process.env.SIGH_DEMO_PASSWORD!,
      Seed__DemoDataPath: path.resolve(HERE, '../docs/referencias'),
      Seed__DefaultCompanyName: 'Usina Serra Grande S.A. (DEMONSTRAÇÃO)',
      RateLimits__Login__PermitLimit: '1000',
    },
    stdout: 'ignore',
    stderr: 'pipe',
  },
  {
    command: 'npm run dev',
    url: 'http://localhost:3000',
    reuseExistingServer: false,
    timeout: 60_000,
  },
];

export default defineConfig({
  testDir: './e2e-demo',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  timeout: 180_000,
  globalTeardown: './e2e/support/globalTeardown.ts',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:3000',
    headless: true,
    // Navegador brasileiro: datas nativas em dd/mm/aaaa e horário de Maceió.
    locale: 'pt-BR',
    timezoneId: 'America/Maceio',
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1,
    trace: 'retain-on-failure',
    // Codespace com /dev/shm de 64 MB: sem isto o Chromium cai ("Target crashed").
    launchOptions: { args: ['--disable-dev-shm-usage', '--lang=pt-BR'], env: { ...process.env, LANG: 'pt_BR.UTF-8' } },
  },
  projects: [
    { name: 'setup', testMatch: /setup\.spec\.ts/, use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1 } },
    {
      name: 'demo',
      testMatch: /(no-english|accessibility|screenshots)\.spec\.ts/,
      dependencies: ['setup'],
      use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1 },
    },
  ],
  webServer: STACK ? undefined : devServers,
});
