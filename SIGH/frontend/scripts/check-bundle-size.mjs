// Falha se o chunk de entrada (o <script type="module"> de dist/index.html) passar do limite.
// Uso: node scripts/check-bundle-size.mjs [limiteEmKB]   (padrão: 500)
import { readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

const limitKb = Number(process.argv[2] ?? 500);
const dist = new URL('../dist/', import.meta.url).pathname;
const html = readFileSync(join(dist, 'index.html'), 'utf8');

const entry = html.match(/<script[^>]+type="module"[^>]+src="\/([^"]+)"/)?.[1];
if (!entry) {
  console.error('Chunk de entrada não encontrado em dist/index.html');
  process.exit(1);
}

const preloads = [...html.matchAll(/<link[^>]+rel="modulepreload"[^>]+href="\/([^"]+)"/g)].map((m) => m[1]);
const kb = (file) => statSync(join(dist, file)).size / 1000;

const entryKb = kb(entry);
const initialKb = [entry, ...preloads].reduce((sum, file) => sum + kb(file), 0);

console.log(`Chunk de entrada: ${entry} = ${entryKb.toFixed(2)} kB (limite ${limitKb} kB)`);
console.log(`JS da carga inicial (entrada + modulepreload): ${initialKb.toFixed(2)} kB`);

if (entryKb > limitKb) {
  console.error(`O chunk de entrada passou de ${limitKb} kB.`);
  process.exit(1);
}
