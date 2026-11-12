import { expect, test, type Page } from '@playwright/test';

// Fluxo completo contra API + SQL Server reais. Precisa de um admin recém-criado pelo seed
// (MustChangePassword = true). Veja e2e/README.md.
const required = (name: string): string => {
  const value = process.env[name];
  if (!value) throw new Error(`Defina a variável de ambiente ${name} (veja e2e/README.md).`);
  return value;
};

const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@sigh.com.br';
const ADMIN_PASSWORD = required('E2E_ADMIN_PASSWORD');
const NEW_PASSWORD = required('E2E_NEW_PASSWORD');

const storageSnapshot = (page: Page) =>
  page.evaluate(() => JSON.stringify({ local: { ...localStorage }, session: { ...sessionStorage } }));

test('admin: login, mandatory password change, cases list, reload keeps the session, logout', async ({ page, context }) => {
  // Nenhuma resposta da API pode trazer o refresh token no corpo.
  const authBodies: string[] = [];
  page.on('response', async (response) => {
    if (response.url().includes('/api/v1/auth/')) {
      authBodies.push(await response.text().catch(() => ''));
    }
  });

  // 1. Login do admin
  await page.goto('/');
  await expect(page).toHaveURL(/\/login$/);
  await page.getByLabel('E-mail').fill(ADMIN_EMAIL);
  await page.getByLabel('Senha').fill(ADMIN_PASSWORD);
  await page.getByRole('button', { name: 'Entrar' }).click();

  // 2. Troca obrigatória de senha: qualquer outra rota volta para /change-password
  await expect(page).toHaveURL(/\/change-password$/);
  await page.goto('/disciplinary/cases');
  await expect(page).toHaveURL(/\/change-password$/);

  await page.getByLabel('Senha atual').fill(ADMIN_PASSWORD);
  await page.getByLabel('Nova senha', { exact: true }).fill(NEW_PASSWORD);
  await page.getByLabel('Confirmar nova senha').fill(NEW_PASSWORD);
  await page.getByRole('button', { name: 'Salvar nova senha' }).click();
  await expect(page).not.toHaveURL(/\/change-password$/);
  await expect(page.getByTestId('current-user-name')).toBeVisible();

  // 3. Lista de processos (a chamada à API tem que funcionar com o token novo)
  const listResponse = page.waitForResponse((r) => new URL(r.url()).pathname === '/api/v1/disciplinary-cases');
  await page.getByRole('navigation', { name: 'Menu principal' }).getByRole('button', { name: 'Processos' }).click();
  await expect(page).toHaveURL(/\/disciplinary\/cases$/);
  expect((await listResponse).status()).toBe(200);
  await expect(page.getByRole('heading', { name: /403/ })).toHaveCount(0);
  await expect(page.getByText(/Processos Disciplinares/i).first()).toBeVisible();

  // O cookie do refresh token é HttpOnly, Secure, SameSite=Strict e restrito a /api/v1/auth.
  const rt = (await context.cookies()).find((c) => c.name === 'sigh_rt');
  expect(rt).toMatchObject({ httpOnly: true, secure: true, sameSite: 'Strict', path: '/api/v1/auth' });

  // Nada de token no armazenamento do navegador nem no corpo das respostas.
  const storage = await storageSnapshot(page);
  expect(storage).not.toContain(rt!.value);
  expect(storage).not.toMatch(/eyJ[\w-]+\.[\w-]+\./); // nenhum JWT
  for (const body of authBodies) {
    expect(body).not.toContain(rt!.value);
    expect(body.toLowerCase()).not.toContain('refreshtoken');
  }

  // 4. Recarregar mantém a sessão (refresh via cookie + /auth/me) sem passar pelo login
  const visitedLogin: string[] = [];
  page.on('framenavigated', (frame) => {
    if (frame === page.mainFrame() && frame.url().includes('/login')) visitedLogin.push(frame.url());
  });
  const reloadListResponse = page.waitForResponse((r) => new URL(r.url()).pathname === '/api/v1/disciplinary-cases');
  await page.reload();
  await expect(page).toHaveURL(/\/disciplinary\/cases$/);
  expect((await reloadListResponse).status()).toBe(200);
  await expect(page.getByTestId('current-user-name')).toBeVisible();
  await expect(page.getByText(/Processos Disciplinares/i).first()).toBeVisible();
  expect(visitedLogin).toEqual([]);

  // 5. Logout: volta para o login, o cookie some e recarregar não restaura a sessão
  await page.getByRole('button', { name: 'Sair' }).click();
  await expect(page).toHaveURL(/\/login$/);
  expect((await context.cookies()).find((c) => c.name === 'sigh_rt')).toBeUndefined();
  await page.goto('/disciplinary/cases');
  await expect(page).toHaveURL(/\/login$/);
});
