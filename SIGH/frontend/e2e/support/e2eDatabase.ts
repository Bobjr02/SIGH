// Banco exclusivo de cada execução do e2e: SIGH_E2E_<timestamp>, criado pela própria API
// (migrations + seed no startup) e apagado no teardown, mesmo quando os testes falham.
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));

export const E2E_DB_PATTERN = /^SIGH_E2E_\d{8,17}$/;
const API_PROJECT = path.resolve(HERE, '../../../backend/src/SIGH.Api');

/** Connection string de dev (user-secrets) ou E2E_BASE_CONNECTION_STRING. */
export const baseConnectionString = (): string => {
  if (process.env.E2E_BASE_CONNECTION_STRING) return process.env.E2E_BASE_CONNECTION_STRING;
  const out = execFileSync('dotnet', ['user-secrets', 'list', '--project', API_PROJECT], { encoding: 'utf8' });
  const line = out.split('\n').find((l) => l.startsWith('ConnectionStrings:DefaultConnection = '));
  if (!line) throw new Error('ConnectionStrings:DefaultConnection não está nos user-secrets da API.');
  return line.slice('ConnectionStrings:DefaultConnection = '.length).trim();
};

export const userSecret = (key: string): string | undefined => {
  try {
    const out = execFileSync('dotnet', ['user-secrets', 'list', '--project', API_PROJECT], { encoding: 'utf8' });
    const line = out.split('\n').find((l) => l.startsWith(`${key} = `));
    return line?.slice(key.length + 3).trim();
  } catch {
    return undefined;
  }
};

export const withDatabase = (connectionString: string, database: string) =>
  /Database=[^;]*/i.test(connectionString)
    ? connectionString.replace(/Database=[^;]*/i, `Database=${database}`)
    : `${connectionString.replace(/;?$/, ';')}Database=${database};`;

const part = (cs: string, key: string) => new RegExp(`(?:^|;)\\s*${key}\\s*=([^;]*)`, 'i').exec(cs)?.[1]?.trim();

/**
 * Apaga um banco SIGH_E2E_* (só esse padrão: nunca um banco de dev). Executa o sqlcmd dentro do
 * container do SQL Server do docker-compose; fecha conexões abertas antes do DROP.
 */
export const dropE2EDatabase = (database: string, connectionString = baseConnectionString()): boolean => {
  if (!E2E_DB_PATTERN.test(database)) throw new Error(`Recusado: ${database} não é um banco de e2e.`);
  const user = part(connectionString, 'User Id') ?? part(connectionString, 'User ID') ?? 'sa';
  const password = part(connectionString, 'Password');
  if (!password) throw new Error('A connection string não tem senha para o sqlcmd.');
  const container = process.env.E2E_SQL_CONTAINER ?? 'sigh-sqlserver';
  const sql =
    `IF DB_ID(N'${database}') IS NOT NULL BEGIN ` +
    `ALTER DATABASE [${database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [${database}]; END`;
  // Senha via variável de ambiente do processo (SQLCMDPASSWORD), não na linha de comando.
  execFileSync('docker', ['exec', '-e', 'SQLCMDPASSWORD', container, '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', user, '-C', '-b', '-Q', sql], {
    env: { ...process.env, SQLCMDPASSWORD: password },
    stdio: 'pipe',
  });
  return true;
};

export const listE2EDatabases = (connectionString = baseConnectionString()): string[] => {
  const user = part(connectionString, 'User Id') ?? 'sa';
  const password = part(connectionString, 'Password')!;
  const container = process.env.E2E_SQL_CONTAINER ?? 'sigh-sqlserver';
  const out = execFileSync(
    'docker',
    ['exec', '-e', 'SQLCMDPASSWORD', container, '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', user, '-C', '-h', '-1', '-W', '-Q',
      "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE name LIKE 'SIGH[_]E2E[_]%'"],
    { env: { ...process.env, SQLCMDPASSWORD: password }, encoding: 'utf8' },
  );
  return out.split('\n').map((l) => l.trim()).filter((l) => E2E_DB_PATTERN.test(l));
};

/** Executa uma consulta escalar no banco DESTA execução do e2e (só SIGH_E2E_*), via sqlcmd no container. */
export const queryE2EScalar = (sql: string, connectionString = baseConnectionString()): string => {
  const database = process.env.E2E_DB_NAME ?? '';
  if (!E2E_DB_PATTERN.test(database)) throw new Error('Consulta permitida só no banco do e2e.');
  const user = part(connectionString, 'User Id') ?? 'sa';
  const password = part(connectionString, 'Password')!;
  const container = process.env.E2E_SQL_CONTAINER ?? 'sigh-sqlserver';
  const out = execFileSync(
    'docker',
    ['exec', '-e', 'SQLCMDPASSWORD', container, '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', user, '-C', '-d', database, '-h', '-1', '-W', '-b', '-Q', `SET NOCOUNT ON; ${sql}`],
    { env: { ...process.env, SQLCMDPASSWORD: password }, encoding: 'utf8' },
  );
  return out.trim().split('\n')[0]?.trim() ?? '';
};
