import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { expect, test } from '@playwright/test';
import { apiToken, authed, createUserApi, exeBytes, isoDay, loginUi, pdfBytes } from './support/helpers';
import { syntheticCpf } from './support/xlsx';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));

const sha256 = (data: Buffer) => createHash('sha256').update(data).digest('hex');

// Anexos das medidas reaproveitam as evidências: a carta assinada (PDF) é anexada pela tela da medida.
test('attach a real PDF to a measure, reject a renamed .exe, download with matching hash, and audit it', async ({ page, request }) => {
  const admin = await apiToken(request);
  const stamp = Date.now();
  const registrar = await createUserApi(request, admin, { fullName: 'Auxiliar Evidências', email: `aux.ev.${stamp}@sigh.local`, cpf: syntheticCpf('402000001'), roles: ['HrRegistrar'] });

  // Medida para o Bruno (60090002, importado pelo spec de funcionários).
  const bruno = (await (await request.get('/api/v1/employees?searchTerm=60090002', { headers: authed(admin) })).json()).data.items[0];
  const infraction = await request.post('/api/v1/infraction-types', {
    headers: authed(admin),
    data: { code: `EV${stamp % 100000}`, name: 'Uso indevido de EPI (E2E)', defaultSeverity: 2 },
  });
  const registered = await request.post('/api/v1/disciplinary-records', {
    headers: authed(registrar.token),
    data: {
      employeeId: bruno.id,
      infractionTypeId: (await infraction.json()).data.id,
      measureType: 3,
      occurrenceDates: [isoDay(-2)],
      measureDate: isoDay(0),
      description: 'Sem óculos de proteção na moenda.',
    },
  });
  expect(registered.status(), await registered.text()).toBe(201);
  const recordId: string = (await registered.json()).data.record.summary.id;

  await loginUi(page, registrar.email, registrar.password);
  await page.goto(`/medidas/${recordId}`);
  const attachments = page.getByRole('region', { name: 'Anexos' });

  // Upload do PDF real pela tela.
  await attachments.getByLabel('Título do anexo').fill('Carta assinada');
  await attachments.getByTestId('attachment-input').setInputFiles({ name: 'carta.pdf', mimeType: 'application/pdf', buffer: pdfBytes() });
  const pdfRow = attachments.getByTestId('attachment').filter({ hasText: 'carta.pdf' });
  await expect(pdfRow).toBeVisible();

  // .exe renomeado para .pdf: o cliente aceita a extensão, o servidor recusa pela assinatura.
  await attachments.getByLabel('Título do anexo').fill('Contrato suspeito');
  await attachments.getByTestId('attachment-input').setInputFiles({ name: 'contrato.pdf', mimeType: 'application/pdf', buffer: exeBytes() });
  await expect(attachments.getByText(/não corresponde a nenhum formato permitido/i)).toBeVisible();
  await expect(attachments.getByTestId('attachment').filter({ hasText: 'Contrato suspeito' })).toContainText('Sem arquivo');

  // Download pela tela: o hash do arquivo baixado bate com o original e com o informado pelo servidor.
  const [download] = await Promise.all([page.waitForEvent('download'), pdfRow.getByRole('button', { name: 'Baixar Carta assinada' }).click()]);
  expect(download.suggestedFilename()).toBe('carta.pdf');
  const downloaded = readFileSync((await download.path())!);
  const expectedHash = sha256(pdfBytes());
  expect(sha256(downloaded)).toBe(expectedHash);

  const detail = (await (await request.get(`/api/v1/disciplinary-records/${recordId}`, { headers: authed(registrar.token) })).json()).data;
  const evidenceId: string = detail.evidences.find((e: { title: string }) => e.title === 'Carta assinada').id;
  const viaApi = await request.get(`/api/v1/disciplinary-cases/${recordId}/evidences/${evidenceId}/file`, { headers: authed(registrar.token) });
  expect(viaApi.headers()['x-content-sha256']).toBe(expectedHash);

  // Auditoria (admin tem Audit.View): registro da medida, arquivo anexado e download.
  const audit = async (query: string) =>
    (await (await request.get(`/api/v1/audit-logs?${query}`, { headers: authed(admin) })).json()).data.items as Array<Record<string, string>>;
  const caseLogs = await audit(`entityName=DisciplinaryCase&entityId=${recordId}`);
  const evidenceLogs = await audit(`entityId=${evidenceId}`);

  const creation = caseLogs.find((l) => l.action === 'Created');
  const attach = evidenceLogs.find((l) => l.action === 'Updated');
  const downloadLog = evidenceLogs.find((l) => l.action === 'Security.EvidenceDownloaded');
  expect(creation).toBeTruthy();
  expect(attach?.newValues).toContain(expectedHash);
  expect(downloadLog?.userName).toBeTruthy();

  const everything = JSON.stringify([...caseLogs, ...evidenceLogs]);
  expect(everything).not.toContain('%PDF');
  expect(everything).not.toMatch(/[0-9a-f]{32}\.pdf/); // caminho físico do storage
  expect(everything).not.toMatch(/PasswordHash"\s*:\s*"\$2/);

  // Exemplos reais para o relatório do sprint.
  const outDir = path.resolve(HERE, '../test-results');
  mkdirSync(outDir, { recursive: true });
  writeFileSync(path.join(outDir, 'audit-examples.json'), JSON.stringify({ creation, attach, download: downloadLog }, null, 2));
});
