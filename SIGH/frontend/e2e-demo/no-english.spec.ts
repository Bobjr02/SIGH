import { expect, test } from '@playwright/test';
import { FORBIDDEN_TERMS, forbiddenIn, login, PROFILES, settle } from './support/demo';
import { DETAILS, menuPaths, openDetail, visibleTextWithTabs } from './support/pages';

// Nenhum termo em inglês no texto visível, em todas as páginas do menu de cada perfil, nos detalhes
// (com todas as abas), nas notificações, na troca de senha e no login. Palavra inteira, sem
// diferenciar maiúsculas. A lista inclui o nome de TODOS os valores de enum (enum-values.json).

test('the forbidden list covers every enum value and the common English UI words', () => {
  expect(FORBIDDEN_TERMS).toEqual(expect.arrayContaining(['PendingApproval', 'JustCauseTermination', 'PendingFirstAccess', 'Completed', 'Not found', 'Loading']));
  expect(forbiddenIn('Situação: Aprovada')).toEqual([]);
  expect(forbiddenIn('status: Completed')).toEqual(['Completed']);
  expect(forbiddenIn('page not found')).toEqual(['Not found']);
  expect(forbiddenIn('SIGH v0.0.0')).toEqual([]);
});

test('login page has no English', async ({ page }) => {
  await page.goto('/login');
  await settle(page);
  expect(forbiddenIn(await page.locator('body').innerText())).toEqual([]);
});

for (const who of PROFILES) {
  test(`${who.label}: no English on any page`, async ({ page }) => {
    const found: string[] = [];
    const check = (where: string, text: string) => {
      for (const term of forbiddenIn(text)) found.push(`${where}: "${term}"`);
    };

    await login(page, who);
    const paths = [...(await menuPaths(page)), '/change-password'];
    if (await page.getByTestId('notification-bell-button').count()) paths.push('/notifications');

    for (const path of paths) {
      await page.goto(path);
      check(path, await visibleTextWithTabs(page));
    }
    for (const detail of DETAILS.filter((d) => paths.includes(d.path))) {
      if (await openDetail(page, detail)) check(detail.name, await visibleTextWithTabs(page));
    }

    expect(found, `termos em inglês para ${who.label}`).toEqual([]);
  });
}
