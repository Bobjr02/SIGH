import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../..');

const MAILPIT = process.env.E2E_MAILPIT_URL ?? 'http://localhost:8025';

const running = (container: string) => {
  try {
    return execFileSync('docker', ['inspect', '-f', '{{.State.Running}}', container], { encoding: 'utf8', stdio: 'pipe' }).trim() === 'true';
  } catch {
    return false;
  }
};

/**
 * Garante o Mailpit para os testes que leem e-mail. Não toca em volumes.
 * - alvo "dev": sobe o Mailpit do docker-compose.yml se ele não estava rodando, e marca
 *   E2E_STARTED_MAILPIT para o globalTeardown remover o container que este setup criou;
 * - alvo "stack" (E2E_TARGET=stack): o Mailpit é o da stack, que o scripts/e2e-stack.sh sobe e desmonta.
 */
export default async function globalSetup() {
  if (process.env.E2E_TARGET !== 'stack' && !running('sigh-mailpit')) {
    try {
      execFileSync('docker', ['compose', 'up', '-d', 'mailpit'], { cwd: ROOT, stdio: 'pipe' });
      process.env.E2E_STARTED_MAILPIT = '1';
    } catch (error) {
      console.warn('[e2e] não foi possível subir o Mailpit via docker compose:', (error as Error).message);
    }
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
