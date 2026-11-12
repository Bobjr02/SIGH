import { expect, test } from '@playwright/test';
import { apiToken, authed, loginUi, waitForEmail } from './support/helpers';

// "Esqueci a senha" de ponta a ponta: o e-mail sai pela outbox, chega ao Mailpit e o link redefine a senha.
test('forgot password: the e-mail reaches Mailpit and its link resets the password', async ({ page, request }) => {
  const stamp = Date.now();
  const email = `e2e.reset.${stamp}@sigh.local`;
  const initialPassword = `Ini#${stamp}Aa`;
  const newPassword = `Nova#${stamp}Bb`;

  // Usuário próprio do teste (o admin dos outros specs não é afetado).
  const token = await apiToken(request);
  const cpf = String(stamp).slice(-11).padStart(11, '7');
  const createUser = await request.post('/api/v1/users', {
    headers: authed(token),
    data: { fullName: 'Pessoa do E2E', email, cpf, initialPassword, roleIds: [] },
  });
  expect(createUser.status(), await createUser.text()).toBe(201);

  // A conta criada recebe o e-mail de primeiro acesso, sem senha.
  const welcome = await waitForEmail(request, email, 'Sua conta no SIGH foi criada');
  expect(welcome.text).not.toContain(initialPassword);

  // Esqueci minha senha, pela tela.
  await page.goto('/login');
  await page.getByRole('link', { name: 'Esqueci minha senha' }).click();
  await expect(page.getByRole('heading', { name: 'Esqueci minha senha' })).toBeVisible();
  await page.getByLabel('E-mail').fill(email);
  await page.getByRole('button', { name: 'Enviar link' }).click();
  await expect(page.getByText(/se o e-mail informado estiver cadastrado/i)).toBeVisible();

  const reset = await waitForEmail(request, email, 'Redefinição de senha');
  const link = /(https?:\/\/\S+\/reset-password\?token=[^\s"<]+)/.exec(reset.text)?.[1];
  expect(link, reset.text).toBeTruthy();
  expect(reset.html).toContain('Redefinir senha');

  // O link (App:PublicBaseUrl) abre a página de redefinição.
  await page.goto(link!);
  await expect(page.getByRole('heading', { name: 'Redefinir senha' })).toBeVisible();
  await page.getByLabel('Nova senha', { exact: true }).fill(newPassword);
  await page.getByLabel('Confirmar nova senha').fill(newPassword);
  await page.getByRole('button', { name: 'Salvar senha' }).click();
  await expect(page.getByText(/senha redefinida com sucesso/i)).toBeVisible();

  // O link é de uso único.
  await page.goto(link!);
  await page.getByLabel('Nova senha', { exact: true }).fill(`Outra#${stamp}Cc`);
  await page.getByLabel('Confirmar nova senha').fill(`Outra#${stamp}Cc`);
  await page.getByRole('button', { name: 'Salvar senha' }).click();
  await expect(page.getByText(/inválido ou expirado/i)).toBeVisible();

  // A senha antiga não entra mais; a nova entra (e não pede troca obrigatória).
  const old = await request.post('/api/v1/auth/login', { data: { email, password: initialPassword } });
  expect(old.status()).toBe(400);
  await loginUi(page, email, newPassword);
  await expect(page).not.toHaveURL(/change-password/);
});
