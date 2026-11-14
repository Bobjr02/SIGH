import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { defineConfig } from '@playwright/test';

// Playwright contra a stack EM CONTAINERS (scripts/local-up), não contra o dev server:
//   npx playwright test -c playwright.local.config.ts
// Lê a senha dos usuários de demonstração do .env.local da raiz (gerado pelo local-up; fora do git).
const HERE = path.dirname(fileURLToPath(import.meta.url));
const envFile = path.resolve(HERE, '../../.env.local');
const settings = Object.fromEntries(
  readFileSync(envFile, 'utf8')
    .split(/\r?\n/)
    .filter((l) => l && !l.startsWith('#') && l.includes('='))
    .map((l) => [l.slice(0, l.indexOf('=')), l.slice(l.indexOf('=') + 1)]),
);
process.env.SIGH_DEMO_PASSWORD ??= settings.SIGH_DEMO_PASSWORD;

export default defineConfig({
  testDir: './e2e-local',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  timeout: 60_000,
  use: {
    baseURL: process.env.SIGH_LOCAL_URL ?? `http://localhost:${settings.SIGH_WEB_PORT ?? '8080'}`,
    headless: true,
    trace: 'retain-on-failure',
  },
  // O cookie do refresh token é Secure: em http://localhost os navegadores o aceitam (contexto seguro).
  projects: [
    { name: 'chrome', use: { browserName: 'chromium', channel: 'chrome' } },
    { name: 'edge', use: { browserName: 'chromium', channel: 'msedge' } },
  ],
});
