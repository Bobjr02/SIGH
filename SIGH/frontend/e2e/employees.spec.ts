import { mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import { ADMIN_EMAIL, adminPassword, apiToken, authed, loginUi } from './support/helpers';
import { queryE2EScalar } from './support/e2eDatabase';
import { sapFile, syntheticCpf, type SapRow } from './support/xlsx';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const XLSX = 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet';

// Dados SINTÉTICOS (gerados aqui; nada é versionado). A Eva tem CPF que começa com zero, enviado como número.
const cpfs = {
  ana: syntheticCpf('301000001'),
  bruno: syntheticCpf('301000002'),
  carla: syntheticCpf('301000003'),
  davi: syntheticCpf('301000004'),
  eva: syntheticCpf('012000005'),
  fabio: syntheticCpf('301000006'),
};

const initial: SapRow[] = [
  { number: '60090001', name: 'Ana E2E Sintética', cpf: cpfs.ana },
  { number: '60090002', name: 'Bruno E2E Sintético', cpf: cpfs.bruno, group: 'Campo', mu: 'USGA141', dept: ['60002000', 'Corte'] },
  { number: '60090003', name: 'Carla E2E Sintética', cpf: cpfs.carla, status: 'Afastado' },
  { number: 60090004, name: 'Davi E2E Sintético', cpf: cpfs.davi },
  { number: '60090005', name: 'Eva E2E Sintética', cpf: Number(cpfs.eva) },
];

const update: SapRow[] = [
  { number: '60090001', name: 'Ana E2E Sintética', cpf: cpfs.ana, job: ['50009999', 'Supervisora'] },
  { number: '60090002', name: 'Bruno E2E Sintético', cpf: cpfs.bruno, group: 'Campo', mu: 'USGA141', dept: ['60002000', 'Corte'] },
  // 60090003 (Carla) ausente → desligada por ausência
  { number: 60090004, name: 'Davi E2E Sintético', cpf: cpfs.davi },
  { number: '60090005', name: 'Eva E2E Sintética', cpf: Number(cpfs.eva) },
  { number: '60090006', name: 'Fábio E2E Sintético', cpf: cpfs.fabio },
];

const importFile = async (page: Page, name: string, rows: SapRow[], { mass }: { mass: boolean }) => {
  await page.goto('/employees/import');
  await page.getByLabel('Planilha do SAP').setInputFiles({ name, mimeType: XLSX, buffer: sapFile(rows) });
  const previewResponse = page.waitForResponse((r) => r.url().endsWith('/api/v1/employees/import/preview'));
  await page.getByRole('button', { name: 'Pré-visualizar' }).click();
  const preview = (await (await previewResponse).json()).data;
  await expect(page.getByRole('region', { name: 'Pré-visualização' })).toBeVisible();

  if (mass) {
    await expect(page.getByRole('button', { name: 'Confirmar importação' })).toBeDisabled();
    await page.getByLabel('Confirmo o desligamento em massa').check();
  }
  await page.getByRole('button', { name: 'Confirmar importação' }).click();
  await expect(page.getByText(/Importação aplicada/)).toBeVisible();
  return preview;
};

/** Processo com uma suspensão de 3 dias aplicada ao funcionário (decisão aprovada por outro usuário). */
const arrangeSuspension = async (request: APIRequestContext, employeeId: string) => {
  const admin = authed(await apiToken(request));
  const post = async (url: string, data: unknown, headers = admin) => {
    const r = await request.post(url, { headers, data });
    expect(r.ok(), `${url}: ${await r.text()}`).toBeTruthy();
    return (await r.json()).data;
  };

  // Aprovador: outro usuário com o perfil Administrator (segregação de funções).
  const roleId = queryE2EScalar("SELECT CAST(Id AS nvarchar(36)) FROM Roles WHERE Name = 'Administrator'");
  const approverEmail = `aprovador.${Date.now()}@sigh.local`;
  const approverInitial = `Apr#${Date.now()}Aa`;
  await post('/api/v1/users', { fullName: 'Aprovador E2E', email: approverEmail, cpf: syntheticCpf('401000009'), initialPassword: approverInitial, roleIds: [roleId] });
  const first = await apiToken(request, approverEmail, approverInitial);
  const changed = await post('/api/v1/auth/change-password', { currentPassword: approverInitial, newPassword: `${approverInitial}9z` }, authed(first));
  const approver = authed(changed.accessToken);

  const infraction = await post('/api/v1/infraction-types', { code: `E2E${Date.now() % 100000}`, name: 'Falta não justificada (E2E)', defaultSeverity: 2 });
  const created = await post('/api/v1/disciplinary-cases', {
    caseNumber: `E2E-EMP-${Date.now()}`.slice(0, 20),
    companyId: '00000000-0000-0000-0000-000000000001',
    title: 'Faltas reiteradas (e2e)',
    description: 'Processo criado pelo e2e de funcionários.',
  });
  const caseId: string = created.id;
  await post(`/api/v1/disciplinary-cases/${caseId}/open`, {});
  await post(`/api/v1/disciplinary-cases/${caseId}/occurrences`, {
    occurrenceDate: new Date().toISOString(), description: 'Faltou três dias sem justificativa.', infractionTypeId: infraction.id, severity: 2,
  });
  await post(`/api/v1/disciplinary-cases/${caseId}/employees`, { disciplinaryCaseId: caseId, employeeId, role: 1, isPrimarySubject: true });
  await post(`/api/v1/disciplinary-cases/${caseId}/start-investigation`, {});
  await post(`/api/v1/disciplinary-cases/${caseId}/submit-for-decision`, {});
  const decision = await post(`/api/v1/disciplinary-cases/${caseId}/decisions`, {
    decisionType: 4, summary: 'Suspensão de 3 dias.', reasoning: 'As faltas foram comprovadas na apuração.',
  });
  await post(`/api/v1/disciplinary-cases/${caseId}/approve-decision`, {}, approver);

  const today = new Date();
  today.setUTCHours(0, 0, 0, 0);
  const until = new Date(today.getTime() + 2 * 24 * 3600 * 1000);
  await post(`/api/v1/disciplinary-cases/${caseId}/measures`, {
    disciplinaryDecisionId: decision.decisionId, employeeId, measureType: 4, reason: 'Suspensão por faltas.',
    effectiveFrom: today.toISOString(), effectiveUntil: until.toISOString(),
  });

  const evidence = await post(`/api/v1/disciplinary-cases/${caseId}/evidences`, {
    evidenceType: 1, title: 'Atestado fotografado', description: 'Foto do documento.', collectedAt: new Date().toISOString(),
  });
  return { caseId, evidenceId: evidence.id as string };
};

// JPEG mínimo válido (SOI + APP0/JFIF + EOI).
const jpeg = () =>
  Buffer.from([0xff, 0xd8, 0xff, 0xe0, 0x00, 0x10, 0x4a, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xff, 0xd9]);

test('SAP import (initial + update), employee with decision support, and a JPG photo as Document evidence', async ({ page, request }) => {
  test.setTimeout(150_000);
  await loginUi(page);

  // 1. Carga inicial.
  const first = await importFile(page, 'funcionarios_carga_inicial.xlsx', initial, { mass: false });
  expect(first.summary).toMatchObject({ created: 5, updated: 0, terminatedByAbsence: 0, errors: 0 });

  // 2. Atualização: 1 alterado, 1 novo, 1 ausente (20% dos ativos > 10%: exige confirmação explícita).
  const second = await importFile(page, 'funcionarios_atualizacao.xlsx', update, { mass: true });
  expect(second.summary).toMatchObject({ created: 1, updated: 1, terminatedByAbsence: 1, reactivated: 0, unchanged: 3, errors: 0 });
  expect(second.toTerminate.map((c: { employeeNumber: string }) => c.employeeNumber)).toEqual(['60090003']);
  await expect(page.getByRole('table', { name: 'Histórico de importações' }).getByText('funcionarios_atualizacao.xlsx')).toBeVisible();

  // Eva: zero à esquerda do CPF preservado; lista mostra CPF mascarado.
  await page.goto('/employees');
  await page.getByLabel('Nome ou matrícula').fill('60090005');
  await page.getByRole('button', { name: 'Filtrar' }).click();
  const evaRow = page.getByTestId('employee-row-60090005');
  await expect(evaRow).toContainText(`***.${cpfs.eva.slice(3, 6)}.${cpfs.eva.slice(6, 9)}-**`);
  await expect(page.getByText(cpfs.eva)).toHaveCount(0);
  await evaRow.click();
  await expect(page.getByTestId('employee-cpf')).toHaveText(
    `${cpfs.eva.slice(0, 3)}.${cpfs.eva.slice(3, 6)}.${cpfs.eva.slice(6, 9)}-${cpfs.eva.slice(9)}`,
  );
  expect(cpfs.eva.startsWith('0')).toBeTruthy();

  // 3. Funcionário com uma medida real → quadro de apoio à decisão.
  const token = await apiToken(request, ADMIN_EMAIL, adminPassword());
  const list = await (await request.get('/api/v1/employees?searchTerm=60090001', { headers: authed(token) })).json();
  const ana = list.data.items[0];
  const { caseId, evidenceId } = await arrangeSuspension(request, ana.id);

  await page.goto(`/employees/${ana.id}`);
  const box = page.getByRole('region', { name: 'Apoio à decisão' });
  await expect(box).toBeVisible();
  await expect(box.getByText('3 dia(s) no total')).toBeVisible();
  await expect(box.getByRole('list', { name: 'Medidas por motivo' })).toContainText('Falta não justificada (E2E): 1');
  await expect(page.getByTestId('timeline-item').first()).toBeVisible();

  // 4. Foto JPG de um documento anexada a uma evidência do tipo Document.
  await page.goto(`/disciplinary/cases/${caseId}`);
  const row = page.getByTestId(`evidence-row-${evidenceId}`);
  await row.getByRole('button', { name: /anexar arquivo/i }).click();
  await page.getByLabel('Arquivo da evidência').setInputFiles({ name: 'atestado.jpg', mimeType: 'image/jpeg', buffer: jpeg() });
  await page.getByRole('button', { name: 'Enviar arquivo' }).click();
  await expect(row.getByText('atestado.jpg')).toBeVisible();

  // Resultado da importação para o relatório.
  const out = path.resolve(HERE, '../test-results');
  mkdirSync(out, { recursive: true });
  writeFileSync(path.join(out, 'employee-import-results.json'), JSON.stringify({ initial: first, update: second }, null, 2));
});
