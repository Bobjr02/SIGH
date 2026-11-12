import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));

const MAILPIT = process.env.E2E_MAILPIT_URL ?? 'http://localhost:8025';

/** Garante o Mailpit (docker compose) para os testes que leem e-mail. Não toca em volumes. */
export default async function globalSetup() {
  try {
    execFileSync('docker', ['compose', 'up', '-d', 'mailpit'], { cwd: path.resolve(HERE, '../../../..'), stdio: 'pipe' });
  } catch (error) {
    console.warn('[e2e] não foi possível subir o Mailpit via docker compose:', (error as Error).message);
  }
  for (let i = 0; i < 30; i++) {
    try {
      if ((await fetch(`${MAILPIT}/api/v1/info`)).ok) return;
    } catch {
      /* ainda subindo */
    }
    await new Promise((r) => setTimeout(r, 1000));
  }
  throw new Error(`Mailpit não respondeu em ${MAILPIT}.`);
}
