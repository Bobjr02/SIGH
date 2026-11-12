import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { expect, test } from '@playwright/test';
import { apiToken, authed, exeBytes, loginUi, pdfBytes } from './support/helpers';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));

const sha256 = (data: Buffer) => createHash('sha256').update(data).digest('hex');

test('create a case, attach a real PDF, reject a renamed .exe, download with matching hash, and audit it', async ({ page, request }) => {
  const token = await apiToken(request);
  const headers = authed(token);

  // Processo + duas evidências de arquivo (metadados) pela API.
  const created = await request.post('/api/v1/disciplinary-cases', {
    headers,
    data: {
      caseNumber: `E2E-${Date.now()}`.slice(0, 20),
      companyId: '00000000-0000-0000-0000-000000000001',
      title: 'Processo do e2e de evidências',
      description: 'Criado pelo teste e2e de evidências.',
    },
  });
  expect(created.status(), await created.text()).toBe(201);
  const caseId: string = (await created.json()).data.id;

  const newEvidence = async (title: string) => {
    const r = await request.post(`/api/v1/disciplinary-cases/${caseId}/evidences`, {
      headers,
      data: { evidenceType: 1, title, description: 'Documento anexado no e2e', collectedAt: new Date().toISOString() },
    });
    expect(r.ok(), await r.text()).toBeTruthy();
    return (await r.json()).data.id as string;
  };
  const pdfEvidence = await newEvidence('Laudo pericial');
  const exeEvidence = await newEvidence('Contrato suspeito');

  await loginUi(page);
  await page.goto(`/disciplinary/cases/${caseId}`);

  // Upload do PDF real pela tela.
  const pdfRow = page.getByTestId(`evidence-row-${pdfEvidence}`);
  await pdfRow.getByRole('button', { name: /anexar arquivo/i }).click();
  await page.getByLabel('Arquivo da evidência').setInputFiles({ name: 'laudo.pdf', mimeType: 'application/pdf', buffer: pdfBytes() });
  await page.getByRole('button', { name: 'Enviar arquivo' }).click();
  await expect(pdfRow.getByText('laudo.pdf')).toBeVisible();
  const expectedHash = sha256(pdfBytes());
  await expect(pdfRow.getByText(`${expectedHash.slice(0, 8)}…${expectedHash.slice(-4)}`)).toBeVisible();

  // .exe renomeado para .pdf: o cliente aceita a extensão, o servidor recusa pela assinatura.
  const exeRow = page.getByTestId(`evidence-row-${exeEvidence}`);
  await exeRow.getByRole('button', { name: /anexar arquivo/i }).click();
  await page.getByLabel('Arquivo da evidência').setInputFiles({ name: 'contrato.pdf', mimeType: 'application/pdf', buffer: exeBytes() });
  await page.getByRole('button', { name: 'Enviar arquivo' }).click();
  await expect(page.getByRole('dialog').getByText(/não corresponde a nenhum formato permitido/i)).toBeVisible();
  await page.getByRole('button', { name: 'Cancelar' }).click();
  await expect(exeRow.getByText('Sem arquivo')).toBeVisible();

  // Download pela tela: o hash do arquivo baixado bate com o gravado no servidor.
  const [download] = await Promise.all([page.waitForEvent('download'), pdfRow.getByRole('button', { name: 'Baixar laudo.pdf' }).click()]);
  expect(download.suggestedFilename()).toBe('laudo.pdf');
  const downloaded = readFileSync((await download.path())!);
  const detail = await (await request.get(`/api/v1/disciplinary-cases/${caseId}`, { headers })).json();
  const recorded = detail.data.evidences.find((e: { id: string }) => e.id === pdfEvidence);
  expect(recorded.integrityHash).toBe(expectedHash);
  expect(sha256(downloaded)).toBe(recorded.integrityHash);
  expect(recorded.contentType).toBe('application/pdf');

  // Auditoria: criação do processo, alteração da evidência (arquivo anexado) e download.
  const audit = async (query: string) =>
    (await (await request.get(`/api/v1/audit-logs?${query}`, { headers })).json()).data.items as Array<Record<string, string>>;
  const caseLogs = await audit(`entityName=DisciplinaryCase&entityId=${caseId}`);
  const evidenceLogs = await audit(`entityId=${pdfEvidence}`);

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

  // A aba "Histórico" mostra a mesma trilha.
  await page.getByRole('tab', { name: 'Histórico' }).click();
  await expect(page.getByText('Download de evidência').first()).toBeVisible();

  // Exemplos reais para o relatório do sprint.
  const outDir = path.resolve(HERE, '../test-results');
  mkdirSync(outDir, { recursive: true });
  writeFileSync(path.join(outDir, 'audit-examples.json'), JSON.stringify({ creation, attach, download: downloadLog }, null, 2));
});
