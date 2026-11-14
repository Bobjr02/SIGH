import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, test } from '@playwright/test';
import { apiToken, authed, createUserApi, isoDay, loginUi, logoutUi } from './support/helpers';
import { syntheticCpf } from './support/xlsx';

// Histórico disciplinar de ponta a ponta, com os arquivos FICTÍCIOS do pacote de referência:
// funcionários → histórico → apoio à decisão da 50090007 (só histórico) → nova medida no fluxo do RH
// → o quadro passa a mostrar histórico + SIGH. Roda por último (a carga do pacote desliga, por ausência,
// os funcionários sintéticos dos outros specs).

const HERE = path.dirname(fileURLToPath(import.meta.url));
const REFS = path.resolve(HERE, '../../docs/referencias');
const EMPLOYEES = path.join(REFS, 'importacao-funcionarios', 'funcionarios_carga_inicial.xlsx');
const HISTORY = path.join(REFS, 'historico-disciplinar', 'base_disciplinar_ficticia.xlsx');

test('import employees and history, decision support of 50090007 with history, then history + SIGH', async ({ page, request }) => {
  test.setTimeout(240_000);
  const admin = await apiToken(request);
  const stamp = Date.now();

  // Catálogo do RH: os três motivos da planilha que já existem (HISTORICO.md); "USO DE CELULAR EM SERVIÇO" é o novo.
  for (const [code, name] of [
    ['HFALTA', 'Faltas injustificadas de forma recorrente'],
    ['HEPI', 'Falta de utilização do EPI'],
    ['HNEG', 'Negligência ou imprudência no desempenho de suas funções'],
  ]) {
    const created = await request.post('/api/v1/infraction-types', { headers: authed(admin), data: { code: `${code}${stamp % 10000}`, name, defaultSeverity: 2 } });
    expect(created.ok(), await created.text()).toBeTruthy();
  }

  // 1. Funcionários (carga inicial do pacote), pela tela do administrador.
  await loginUi(page);
  await page.goto('/employees/import');
  await page.getByLabel('Planilha do SAP').setInputFiles(EMPLOYEES);
  await page.getByRole('button', { name: 'Pré-visualizar' }).click();
  await expect(page.getByRole('region', { name: 'Pré-visualização' })).toBeVisible();
  const mass = page.getByLabel('Confirmo o desligamento em massa');
  if (await mass.isVisible()) await mass.check();
  await page.getByRole('button', { name: 'Confirmar importação' }).click();
  await expect(page.getByText(/Importação aplicada/)).toBeVisible();

  // 2. Histórico disciplinar, pela tela /admin/history-import.
  await page.getByRole('navigation', { name: 'Menu principal' }).getByRole('button', { name: 'Importar histórico' }).click();
  await page.getByLabel('Planilha de penalidades').setInputFiles(HISTORY);
  await page.getByRole('button', { name: 'Pré-visualizar' }).click();
  await expect(page.getByTestId('history-summary')).toContainText('16 linhas com dados: 14 a importar');
  await expect(page.getByTestId('history-summary')).toContainText('2 rejeitadas');
  await page.getByRole('tab', { name: 'Motivos novos (1)' }).click();
  await expect(page.getByRole('list', { name: 'Motivos novos' })).toContainText('USO DE CELULAR EM SERVIÇO');
  await page.getByRole('button', { name: 'Confirmar importação' }).click();
  await expect(page.getByText(/Histórico importado: 14 registro\(s\) novo\(s\)/)).toBeVisible();
  await expect(page.getByTestId('history-batch').first()).toContainText('Aplicado');

  // 3. Apoio à decisão da 50090007: 1 advertência, 2 suspensões, 4 dias, tudo histórico.
  const employees = await (await request.get('/api/v1/employees?searchTerm=50090007', { headers: authed(admin) })).json();
  const employee = employees.data.items[0];
  await page.goto(`/employees/${employee.id}`);
  const support = page.getByRole('region', { name: 'Apoio à decisão' });
  await expect(support.getByText('4 dia(s) no total')).toBeVisible();
  const origins = support.getByRole('list', { name: 'Medidas por origem' });
  await expect(origins).toContainText('1 advertências — 1 histórica, 0 no SIGH');
  await expect(origins).toContainText('2 suspensões — 2 históricas, 0 no SIGH');
  await expect(page.getByTestId('history-seal').filter({ hasText: 'Histórico importado — lote de' }).first()).toBeVisible();

  // 4. Nova medida para ela no fluxo do sprint 6: o auxiliar registra, o encarregado aprova.
  const registrar = await createUserApi(request, admin, { fullName: 'Auxiliar Histórico E2E', email: `aux.hist.${stamp}@sigh.local`, cpf: syntheticCpf('404000001'), roles: ['HrRegistrar'] });
  const approver = await createUserApi(request, admin, { fullName: 'Encarregado Histórico E2E', email: `enc.hist.${stamp}@sigh.local`, cpf: syntheticCpf('404000002'), roles: ['HrApprover'] });

  await logoutUi(page);
  await loginUi(page, registrar.email, registrar.password);
  await page.goto('/medidas/registrar');
  await page.getByRole('combobox', { name: /funcionário/i }).fill('50090007');
  await page.getByRole('option', { name: /50090007/ }).click();
  // O quadro do registro já mostra o histórico.
  await expect(page.getByRole('region', { name: 'Apoio à decisão' }).getByRole('list', { name: 'Medidas por origem' })).toContainText('histórica');
  await page.getByRole('combobox', { name: /motivo/i }).click();
  await page.getByRole('option', { name: 'Faltas injustificadas de forma recorrente' }).click();
  await page.getByRole('combobox', { name: /tipo de medida/i }).click();
  await page.getByRole('option', { name: 'Advertência escrita' }).click();
  await page.getByLabel('Ocorrência 1').fill(isoDay(-2));
  await page.getByLabel(/data da medida/i).fill(isoDay(0));
  await page.getByLabel(/descrição/i).fill('Faltou de novo, sem justificativa.');
  await page.getByRole('form', { name: 'Registrar medida' }).getByRole('button', { name: 'Registrar medida' }).click();
  await expect(page.getByText('Medida registrada. Aguardando aprovação.')).toBeVisible();
  const recordId = page.url().split('/').pop()!;
  const approved = await request.post(`/api/v1/disciplinary-records/${recordId}/approve`, { headers: authed(approver.token) });
  expect(approved.ok(), await approved.text()).toBeTruthy();

  // 5. O quadro passa a mostrar histórico + SIGH.
  await logoutUi(page);
  await loginUi(page);
  await page.goto(`/employees/${employee.id}`);
  const after = page.getByRole('region', { name: 'Apoio à decisão' }).getByRole('list', { name: 'Medidas por origem' });
  await expect(after).toContainText('2 advertências — 1 histórica, 1 no SIGH');
  await expect(after).toContainText('2 suspensões — 2 históricas, 0 no SIGH');
});
