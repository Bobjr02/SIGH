import { dropE2EDatabase } from './e2eDatabase';

/** Roda depois de todos os testes, passando ou falhando: apaga o banco desta execução. */
export default async function globalTeardown() {
  const database = process.env.E2E_DB_NAME;
  if (!database || process.env.E2E_KEEP_DB === '1') return;
  try {
    dropE2EDatabase(database);
    console.log(`[e2e] banco ${database} apagado.`);
  } catch (error) {
    console.error(`[e2e] não foi possível apagar ${database}:`, (error as Error).message);
  }
}
