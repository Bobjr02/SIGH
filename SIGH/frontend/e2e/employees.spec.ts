import { mkdirSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test, type APIRequestContext, type Page } from '@playwright/test';
import { ADMIN_EMAIL, adminPassword, apiToken, authed, createUserApi, isoDay, loginUi, logoutUi } from './support/helpers';
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

/**
 * Suspensão de 3 dias registrada pelo auxiliar (RH – Registro) e aprovada pelo encarregado (RH – Aprovador)
 * pelo fluxo de medidas: o mesmo caminho do dia a dia do RH.
 */
const arrangeSuspension = async (request: APIRequestContext, employeeId: string) => {
  const admin = await apiToken(request);
  const stamp = Date.now();
  const registrar = await createUserApi(request, admin, { fullName: 'Auxiliar E2E', email: `aux.emp.${stamp}@sigh.local`, cpf: syntheticCpf('401000008'), roles: ['HrRegistrar'] });
  const approver = await createUserApi(request, admin, { fullName: 'Encarregado E2E', email: `enc.emp.${stamp}@sigh.local`, cpf: syntheticCpf('401000009'), roles: ['HrApprover'] });

  const infraction = await request.post('/api/v1/infraction-types', {
    headers: authed(admin),
    data: { code: `E2E${stamp % 100000}`, name: 'Falta não justificada (E2E)', defaultSeverity: 2 },
  });
  expect(infraction.ok(), await infraction.text()).toBeTruthy();

  const registered = await request.post('/api/v1/disciplinary-records', {
    headers: authed(registrar.token),
    data: {
      employeeId,
      infractionTypeId: (await infraction.json()).data.id,
      measureType: 4,
      occurrenceDates: [isoDay(-1)],
      measureDate: isoDay(0),
      suspensionStart: isoDay(0),
      suspensionEnd: isoDay(2),
      description: 'Faltou três dias sem justificativa.',
    },
  });
  expect(registered.status(), await registered.text()).toBe(201);
  const recordId = (await registered.json()).data.record.summary.id as string;

  const approved = await request.post(`/api/v1/disciplinary-records/${recordId}/approve`, { headers: authed(approver.token) });
  expect(approved.ok(), await approved.text()).toBeTruthy();
  return { recordId, registrar };
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
  const { recordId, registrar } = await arrangeSuspension(request, ana.id);

  await page.goto(`/employees/${ana.id}`);
  const box = page.getByRole('region', { name: 'Apoio à decisão' });
  await expect(box).toBeVisible();
  await expect(box.getByText('3 dia(s) no total')).toBeVisible();
  await expect(box.getByRole('list', { name: 'Medidas por motivo' })).toContainText('Falta não justificada (E2E): 1');
  await expect(page.getByTestId('timeline-item').first()).toBeVisible();

  // 4. Foto JPG do documento assinado anexada à medida aprovada (pelo auxiliar, que pode anexar).
  await logoutUi(page);
  await loginUi(page, registrar.email, registrar.password);
  await page.goto(`/medidas/${recordId}`);
  const attachments = page.getByRole('region', { name: 'Anexos' });
  await attachments.getByLabel('Título do anexo').fill('Atestado fotografado');
  await attachments.getByTestId('attachment-input').setInputFiles({ name: 'atestado.jpg', mimeType: 'image/jpeg', buffer: jpeg() });
  await expect(attachments.getByTestId('attachment').filter({ hasText: 'atestado.jpg' })).toBeVisible();

  // Resultado da importação para o relatório.
  const out = path.resolve(HERE, '../test-results');
  mkdirSync(out, { recursive: true });
  writeFileSync(path.join(out, 'employee-import-results.json'), JSON.stringify({ initial: first, update: second }, null, 2));
});
