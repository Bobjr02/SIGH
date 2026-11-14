import { expect, test, type Page } from '@playwright/test';

// Stack local em containers (web nginx em :8080 → API), com os dados de DEMONSTRAÇÃO do Seed:DemoData.

const password = () => {
  const value = process.env.SIGH_DEMO_PASSWORD;
  if (!value) throw new Error('SIGH_DEMO_PASSWORD não encontrada (rode scripts/local-up para gerar o .env.local).');
  return value;
};

const login = async (page: Page, login: string) => {
  await page.goto('/login');
  await expect(page.getByTestId('demo-banner')).toHaveText(/AMBIENTE DE DEMONSTRAÇÃO/);
  await page.getByLabel('E-mail').fill(`${login}@demo.local`);
  await page.getByLabel('Senha').fill(password());
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByTestId('current-user-name')).toBeVisible();
  await expect(page).not.toHaveURL(/change-password/);
};

/** Gerência (3ª coluna) de cada linha visível de uma tabela de medidas. */
const unitsOf = async (page: Page, scope = page.locator('body')) => {
  const rows = scope.getByTestId('record-row');
  await expect(rows.first()).toBeVisible();
  return rows.evaluateAll((trs) => trs.map((tr) => (tr as HTMLTableRowElement).cells[2].textContent?.trim() ?? ''));
};

test('the Secure refresh cookie works on http://localhost: reload keeps the session', async ({ page, context }) => {
  await login(page, 'juridico');
  const cookie = (await context.cookies()).find((c) => c.name === 'sigh_rt');
  expect(cookie).toMatchObject({ secure: true, httpOnly: true, sameSite: 'Strict', path: '/api/v1/auth' });

  const refresh = page.waitForResponse((r) => r.url().endsWith('/api/v1/auth/refresh'));
  await page.reload();
  expect((await refresh).status()).toBe(200);
  await expect(page.getByTestId('current-user-name')).toBeVisible();
  await expect(page).not.toHaveURL(/\/login/);
});

test('auxiliar (RH – Registro): home with own records, the rejected one highlighted', async ({ page }) => {
  await login(page, 'auxiliar');
  const mine = page.getByTestId('home-mine');
  await expect(mine).toBeVisible();
  await expect(mine.getByTestId('record-status').filter({ hasText: 'Rejeitada' })).toBeVisible();
  await expect(page.getByTestId('home-pending')).toHaveCount(0);
  await expect(page.getByRole('button', { name: /registrar medida/i }).first()).toBeVisible();
});

test('encarregado (RH – Aprovador): home with the pending queue and the counter', async ({ page }) => {
  await login(page, 'encarregado');
  const pending = page.getByTestId('home-pending');
  await expect(pending.getByTestId('record-row')).toHaveCount(2);
  await expect(page.getByTestId('pending-approvals-count')).toContainText('2');
  await expect(page.getByTestId('home-mine')).toHaveCount(0);
});

test('juridico: home with the general list (every management unit)', async ({ page }) => {
  await login(page, 'juridico');
  const all = page.getByTestId('home-all');
  await expect(all.getByRole('heading', { name: 'Medidas', exact: true })).toBeVisible();
  const units = new Set(await unitsOf(page, all));
  expect(units.size).toBeGreaterThan(2);
});

test('gestor.industrial: only USGA131, never USGA137/USGA141', async ({ page }) => {
  await login(page, 'gestor.industrial');
  const all = page.getByTestId('home-all');
  await expect(all.getByRole('heading', { name: 'Medidas das suas gerências' })).toBeVisible();
  expect(new Set(await unitsOf(page, all))).toEqual(new Set(['USGA131']));

  await page.goto('/medidas');
  const units = await unitsOf(page);
  expect(units.every((u) => u === 'USGA131')).toBeTruthy();
  expect(units).not.toContain('USGA137');
  expect(units).not.toContain('USGA141');

  // O filtro de gerência só oferece a gerência dele.
  await page.getByRole('combobox', { name: /gerência/i }).click();
  const options = await page.getByRole('listbox').getByRole('option').allTextContents();
  expect(options.filter((o) => o.startsWith('USGA'))).toEqual([expect.stringMatching(/^USGA131/)]);
});

test('gestor.agricola: USGA137 and USGA141 only', async ({ page }) => {
  await login(page, 'gestor.agricola');
  const all = page.getByTestId('home-all');
  await expect(all.getByRole('heading', { name: 'Medidas das suas gerências' })).toBeVisible();
  const units = await unitsOf(page, all);
  expect(units.every((u) => u === 'USGA137' || u === 'USGA141')).toBeTruthy();
});

test('preposto (Registro + Aprovador): pending queue and own records', async ({ page }) => {
  await login(page, 'preposto');
  await expect(page.getByTestId('home-pending').getByTestId('record-row')).toHaveCount(2);
  await expect(page.getByTestId('home-mine')).toBeVisible();
  await expect(page.getByTestId('home-mine').getByTestId('records-empty')).toBeVisible();
});
