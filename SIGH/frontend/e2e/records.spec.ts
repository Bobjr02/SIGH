import { expect, test, type APIRequestContext, type Browser, type Page } from '@playwright/test';
import { apiToken, authed, createUserApi, isoDay, loginUi, waitForEmail } from './support/helpers';
import { syntheticCpf } from './support/xlsx';

// Fluxo completo do RH (Parte E): perfis, registro → rejeição → correção → aprovação, escopo por
// gerência, administrador sem aprovação, segregação de funções e perda de acesso imediata.
// Usa os funcionários SINTÉTICOS importados pelo spec de funcionários (Davi, 60090004, na USGA131).

const stamp = Date.now();
const people = {
  auxiliar: { fullName: 'Auxiliar de Escritório E2E', email: `auxiliar.${stamp}@sigh.local`, cpf: syntheticCpf('403000001'), role: 'RH – Registro' },
  encarregado: { fullName: 'Encarregado do RH E2E', email: `encarregado.${stamp}@sigh.local`, cpf: syntheticCpf('403000002'), role: 'RH – Aprovador' },
  gestor131: { fullName: 'Gestor da Industrial E2E', email: `gestor131.${stamp}@sigh.local`, cpf: syntheticCpf('403000003'), role: 'Gestor', unit: 'USGA131' },
};
const PASSWORD = `Senha#${stamp}Ok`;

const newPage = async (browser: Browser) => (await browser.newContext()).newPage();

/** a. O administrador cria o usuário pela tela; a pessoa define a senha pelo link do e-mail. */
const createByAdmin = async (admin: Page, request: APIRequestContext, person: { fullName: string; email: string; cpf: string; role: string; unit?: string }) => {
  await admin.goto('/admin/usuarios');
  await admin.getByRole('button', { name: 'Novo usuário' }).click();
  const dialog = admin.getByRole('dialog', { name: 'Novo usuário' });
  await dialog.getByLabel('Nome completo').fill(person.fullName);
  await dialog.getByLabel('E-mail').fill(person.email);
  await dialog.getByLabel('CPF').fill(person.cpf);
  await dialog.getByRole('checkbox', { name: person.role }).check();
  if (person.unit) await dialog.getByRole('checkbox', { name: person.unit }).check();
  await dialog.getByRole('button', { name: 'Criar usuário' }).click();
  await expect(admin.getByText(/e-mail de definição de senha foi enviado/)).toBeVisible();
  const userId = admin.url().split('/').pop()!;

  const mail = await waitForEmail(request, person.email, 'Sua conta no SIGH foi criada');
  const link = /(https?:\/\/\S+\/reset-password\?token=[^\s"<]+)/.exec(mail.text)?.[1];
  expect(link, mail.text).toBeTruthy();
  const own = await admin.context().browser()!.newContext();
  const page = await own.newPage();
  await page.goto(link!);
  await page.getByLabel('Nova senha', { exact: true }).fill(PASSWORD);
  await page.getByLabel('Confirmar nova senha').fill(PASSWORD);
  await page.getByRole('button', { name: 'Salvar senha' }).click();
  await expect(page.getByText(/senha (definida|redefinida) com sucesso/i)).toBeVisible();
  await own.close();
  return userId;
};

const choose = async (page: Page, label: string | RegExp, option: string | RegExp) => {
  await page.getByRole('combobox', { name: label }).click();
  await page.getByRole('option', { name: option }).click();
};

test('records: register, reject, correct, approve, manager scope, admin cannot approve, segregation and immediate loss of access', async ({ browser, request }) => {
  test.setTimeout(300_000);
  const adminToken = await apiToken(request);
  const davi = (await (await request.get('/api/v1/employees?searchTerm=60090004', { headers: authed(adminToken) })).json()).data.items[0];
  expect(davi.managementUnitCode).toBe('USGA131');
  const infraction = await request.post('/api/v1/infraction-types', {
    headers: authed(adminToken),
    data: { code: `REC${stamp % 100000}`, name: 'Atraso reiterado (E2E)', defaultSeverity: 2 },
  });
  expect(infraction.ok(), await infraction.text()).toBeTruthy();

  // a. O administrador cria auxiliar, encarregado e gestor da USGA131.
  const admin = await newPage(browser);
  await loginUi(admin);
  await createByAdmin(admin, request, people.auxiliar);
  await createByAdmin(admin, request, people.encarregado);
  const gestor131Id = await createByAdmin(admin, request, people.gestor131);
  await admin.goto(`/admin/usuarios/${gestor131Id}`);
  await expect(admin.getByTestId('unit-row')).toContainText('USGA131');

  // b. O auxiliar registra uma advertência para o Davi (USGA131).
  const auxiliar = await newPage(browser);
  await loginUi(auxiliar, people.auxiliar.email, PASSWORD);
  await auxiliar.getByRole('button', { name: 'Registrar medida' }).first().click();
  await auxiliar.getByRole('combobox', { name: /funcionário/i }).fill('60090004');
  await auxiliar.getByRole('option', { name: /60090004/ }).click();
  await expect(auxiliar.getByRole('region', { name: 'Apoio à decisão' })).toBeVisible();
  await choose(auxiliar, /motivo/i, 'Atraso reiterado (E2E)');
  await choose(auxiliar, /tipo de medida/i, 'Advertência escrita');
  await auxiliar.getByLabel('Ocorrência 1').fill(isoDay(-3));
  await auxiliar.getByLabel(/data da medida/i).fill(isoDay(0));
  await auxiliar.getByLabel(/descrição/i).fill('Chegou atrasado três vezes na semana.');
  await auxiliar.getByRole('form', { name: 'Registrar medida' }).getByRole('button', { name: 'Registrar medida' }).click();
  await expect(auxiliar.getByText('Medida registrada. Aguardando aprovação.')).toBeVisible();
  await expect(auxiliar.getByTestId('record-status')).toHaveText('Aguardando aprovação');
  const recordId = auxiliar.url().split('/').pop()!;

  // c. O encarregado rejeita; o auxiliar corrige; o encarregado aprova.
  const encarregado = await newPage(browser);
  await loginUi(encarregado, people.encarregado.email, PASSWORD);
  // O contador do menu conta as pendências que ele pode aprovar (outros specs podem ter deixado alguma).
  await expect(encarregado.getByTestId('pending-approvals-count')).toContainText(/[1-9]/);
  await encarregado.getByRole('button', { name: /aprovações pendentes/i }).click();
  await encarregado.getByTestId('record-row').filter({ hasText: '60090004' }).getByRole('link').click();
  await expect(encarregado).toHaveURL(new RegExp(`/medidas/${recordId}$`));
  await encarregado.getByRole('button', { name: 'Rejeitar' }).click();
  await encarregado.getByLabel('Motivo da rejeição').fill('A ocorrência foi na sexta-feira, não na quinta.');
  await encarregado.getByRole('button', { name: 'Confirmar' }).click();
  await expect(encarregado.getByTestId('record-status')).toHaveText('Rejeitada');

  await auxiliar.goto('/');
  const mine = auxiliar.getByTestId('home-mine');
  await expect(mine.getByTestId('record-status').first()).toHaveText('Rejeitada');
  await auxiliar.goto(`/medidas/${recordId}`);
  await expect(auxiliar.getByTestId('rejection-reason')).toContainText('sexta-feira');
  await auxiliar.getByRole('button', { name: 'Corrigir e reenviar' }).click();
  await expect(auxiliar.getByLabel('Ocorrência 1')).toHaveValue(isoDay(-3));
  await auxiliar.getByLabel('Ocorrência 1').fill(isoDay(-2));
  await auxiliar.getByRole('button', { name: 'Reenviar para aprovação' }).click();
  await expect(auxiliar.getByText('Medida corrigida e reenviada para aprovação.')).toBeVisible();

  await encarregado.goto(`/medidas/${recordId}`);
  await encarregado.getByRole('button', { name: 'Aprovar' }).click();
  await expect(encarregado.getByText('Medida aprovada.')).toBeVisible();
  await expect(encarregado.getByTestId('record-status')).toHaveText('Aprovada');

  // d. O gestor da USGA131 vê a medida; um gestor da USGA132 não (lista vazia; detalhe 404).
  const gestor131 = await newPage(browser);
  await loginUi(gestor131, people.gestor131.email, PASSWORD);
  await gestor131.goto('/medidas');
  await expect(gestor131.getByTestId('record-row').filter({ hasText: '60090004' })).toBeVisible();
  await gestor131.goto(`/medidas/${recordId}`);
  await expect(gestor131.getByTestId('record-status')).toHaveText('Aprovada');

  const gestor132User = await createUserApi(request, adminToken, {
    fullName: 'Gestor da Manutenção E2E', email: `gestor132.${stamp}@sigh.local`, cpf: syntheticCpf('403000004'), roles: ['Manager'], units: ['USGA132'],
  });
  const gestor132 = await newPage(browser);
  await loginUi(gestor132, gestor132User.email, gestor132User.password);
  await gestor132.goto('/medidas');
  await expect(gestor132.getByTestId('records-empty')).toBeVisible();
  await gestor132.goto(`/medidas/${recordId}`);
  await expect(gestor132.getByTestId('record-not-found')).toHaveText('Medida não encontrada.');
  expect((await request.get(`/api/v1/disciplinary-records/${recordId}`, { headers: authed(gestor132User.token) })).status()).toBe(404);

  // e. O administrador não vê o botão de aprovar, e a API recusa se ele tentar.
  const pending = await request.post('/api/v1/disciplinary-records', {
    headers: authed((await apiToken(request, people.auxiliar.email, PASSWORD))),
    data: { employeeId: davi.id, infractionTypeId: (await infraction.json()).data.id, measureType: 2, occurrenceDates: [isoDay(-1)], measureDate: isoDay(0), description: 'Advertência verbal por atraso.' },
  });
  expect(pending.status(), await pending.text()).toBe(201);
  const pendingId = (await pending.json()).data.record.summary.id as string;
  await admin.goto(`/medidas/${pendingId}`);
  await expect(admin.getByTestId('record-status')).toHaveText('Aguardando aprovação');
  await expect(admin.getByRole('button', { name: 'Aprovar' })).toHaveCount(0);
  expect((await request.post(`/api/v1/disciplinary-records/${pendingId}/approve`, { headers: authed(adminToken) })).status()).toBe(403);

  // f. Um usuário com os dois perfis registra uma medida e não consegue aprová-la.
  const dual = await createUserApi(request, adminToken, {
    fullName: 'Registro e Aprovação E2E', email: `duplo.${stamp}@sigh.local`, cpf: syntheticCpf('403000005'), roles: ['HrRegistrar', 'HrApprover'],
  });
  const own = await request.post('/api/v1/disciplinary-records', {
    headers: authed(dual.token),
    data: { employeeId: davi.id, infractionTypeId: (await infraction.json()).data.id, measureType: 3, occurrenceDates: [isoDay(-4)], measureDate: isoDay(0), description: 'Registrada por quem também aprova.' },
  });
  expect(own.status(), await own.text()).toBe(201);
  const ownId = (await own.json()).data.record.summary.id as string;
  expect((await request.post(`/api/v1/disciplinary-records/${ownId}/approve`, { headers: authed(dual.token) })).status()).toBe(403);
  const dualPage = await newPage(browser);
  await loginUi(dualPage, dual.email, dual.password);
  await dualPage.goto(`/medidas/${ownId}`);
  await expect(dualPage.getByTestId('record-status')).toHaveText('Aguardando aprovação');
  await expect(dualPage.getByRole('button', { name: 'Aprovar' })).toHaveCount(0);

  // g. O administrador desativa o gestor da USGA131, que perde o acesso na requisição seguinte ao TTL.
  await gestor131.goto('/medidas');
  await expect(gestor131.getByTestId('record-row').first()).toBeVisible();
  await admin.goto(`/admin/usuarios/${gestor131Id}`);
  await admin.getByRole('button', { name: 'Desativar' }).click();
  await expect(admin.getByText('Situação do usuário atualizada.')).toBeVisible();
  // TTL do cache do SecurityStamp no e2e: Auth__SecurityStampCacheSeconds=2 (playwright.config).
  await gestor131.waitForTimeout(3_000);
  await gestor131.getByRole('navigation', { name: 'Menu principal' }).getByRole('button', { name: 'Funcionários' }).click();
  await expect(gestor131).toHaveURL(/\/login$/);
});
