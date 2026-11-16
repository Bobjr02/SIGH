import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { dropE2EDatabase } from './e2eDatabase';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../..');

/**
 * Roda depois de todos os testes, passando ou falhando: apaga o banco desta execução e o container
 * do Mailpit que o globalSetup criou (nunca volumes). No alvo "stack", containers e imagens
 * temporários são removidos pelo scripts/e2e-stack.sh.
 */
export default async function globalTeardown() {
  const database = process.env.E2E_DB_NAME;
  if (database && process.env.E2E_KEEP_DB !== '1') {
    try {
      dropE2EDatabase(database);
      console.log(`[e2e] banco ${database} apagado.`);
    } catch (error) {
      console.error(`[e2e] não foi possível apagar ${database}:`, (error as Error).message);
    }
  }
  if (process.env.E2E_STARTED_MAILPIT === '1') {
    try {
      // "rm -s -f" sem "-v": remove só o container criado pelo setup.
      execFileSync('docker', ['compose', 'rm', '-s', '-f', 'mailpit'], { cwd: ROOT, stdio: 'pipe' });
      console.log('[e2e] container do Mailpit criado pelo e2e removido.');
    } catch (error) {
      console.error('[e2e] não foi possível remover o Mailpit:', (error as Error).message);
    }
  }
}
