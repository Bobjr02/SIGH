// Regenera src/auth/permission-codes.json e as constantes de src/auth/permissions.ts a partir de
// PermissionCodes.cs (backend), na ordem de PermissionCodes.All.
// Uso: node scripts/sync-permission-codes.mjs   (a partir de SIGH/frontend)
// Os testes PermissionCodesFrontendSyncTests (backend) e permissions.test.ts (front) conferem o resultado.
import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const here = path.dirname(fileURLToPath(import.meta.url));
const frontend = path.resolve(here, '..');
const source = readFileSync(path.resolve(frontend, '../backend/src/SIGH.Application/Common/Authorization/PermissionCodes.cs'), 'utf8');

const constants = {};
let currentClass = null;
for (const line of source.split('\n')) {
  const cls = line.match(/public static class (\w+)/);
  if (cls) currentClass = cls[1];
  const constant = line.match(/public const string (\w+) = "([^"]+)"/);
  if (constant) constants[`${currentClass}.${constant[1]}`] = constant[2];
}

const all = [...source.split('All { get; }')[1].matchAll(/new\((\w+\.\w+),/g)].map((m) => {
  const code = constants[m[1]];
  if (!code) throw new Error(`Constante não encontrada: ${m[1]}`);
  return code;
});

writeFileSync(path.join(frontend, 'src/auth/permission-codes.json'), JSON.stringify(all, null, 2) + '\n');

const key = (code) => code.replace(/\./g, '_').replace(/([a-z])([A-Z])/g, '$1_$2').toUpperCase();
const tsPath = path.join(frontend, 'src/auth/permissions.ts');
const header = readFileSync(tsPath, 'utf8').split('export const PERMISSIONS')[0];
writeFileSync(
  tsPath,
  header +
    'export const PERMISSIONS = {\n' +
    all.map((c) => `  ${key(c)}: '${c}',\n`).join('') +
    '} as const;\n\nexport type PermissionCode = (typeof PERMISSIONS)[keyof typeof PERMISSIONS];\n',
);

console.log(`${all.length} permissões sincronizadas.`);
