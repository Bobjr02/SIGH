import { expect, test } from '@playwright/test';
import { ADMIN_EMAIL } from './support/demo';

// Primeiro acesso do administrador do seed (troca obrigatória de senha) no banco descartável.
test('admin: first access with the mandatory password change', async ({ page }) => {
  await page.goto('/login');
  await page.getByLabel('E-mail').fill(ADMIN_EMAIL);
  await page.getByLabel('Senha', { exact: true }).fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page).toHaveURL(/\/change-password$/);
  await page.getByLabel('Senha atual').fill(process.env.E2E_ADMIN_PASSWORD!);
  await page.getByLabel('Nova senha', { exact: true }).fill(process.env.E2E_NEW_PASSWORD!);
  await page.getByLabel('Confirmar nova senha').fill(process.env.E2E_NEW_PASSWORD!);
  await page.getByRole('button', { name: 'Salvar nova senha' }).click();
  await expect(page.getByTestId('current-user-name')).toBeVisible();
});
