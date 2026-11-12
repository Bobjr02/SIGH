import { expect, test, type BrowserContext, type Page } from '@playwright/test';
import { loginUi } from './support/helpers';

// Duas abas do mesmo navegador compartilham o cookie sigh_rt. Recarregar as duas ao mesmo tempo
// faz as duas restaurarem a sessão (POST /auth/refresh) juntas; antes isso era tratado como reuso
// de token e derrubava todas as sessões.

const refreshStatuses = (page: Page, sink: number[]) =>
  page.on('response', (r) => {
    if (r.url().endsWith('/api/v1/auth/refresh')) sink.push(r.status());
  });

const expectLoggedIn = async (page: Page) => {
  await expect(page).toHaveURL(/\/disciplinary\/cases$/);
  await expect(page.getByTestId('current-user-name')).toBeVisible();
};

const twoTabsReloadingTogether = async (context: BrowserContext) => {
  const first = await context.newPage();
  await loginUi(first);
  await first.goto('/disciplinary/cases');
  const second = await context.newPage();
  await second.goto('/disciplinary/cases');
  await expectLoggedIn(second);

  const statuses: number[] = [];
  refreshStatuses(first, statuses);
  refreshStatuses(second, statuses);

  // Recarrega as duas abas ao mesmo tempo, três vezes.
  for (let round = 0; round < 3; round++) {
    await Promise.all([first.reload(), second.reload()]);
    await Promise.all([expectLoggedIn(first), expectLoggedIn(second)]);
  }

  // Uma terceira aba ainda entra sem login: nada foi revogado.
  const third = await context.newPage();
  await third.goto('/disciplinary/cases');
  await expectLoggedIn(third);

  return statuses;
};

test('two tabs reloading at the same time keep the session (Web Locks + BroadcastChannel)', async ({ context }) => {
  const statuses = await twoTabsReloadingTogether(context);

  expect(statuses.length).toBeGreaterThan(0);
  expect(statuses.every((s) => s === 200)).toBeTruthy();
});

test('without the Web Locks API, the backend grace window still keeps both tabs signed in', async ({ browser }) => {
  // Simula um navegador sem navigator.locks: as duas abas mandam o MESMO cookie quase juntas.
  const context = await browser.newContext();
  await context.addInitScript(() => {
    Object.defineProperty(Navigator.prototype, 'locks', { get: () => undefined, configurable: true });
  });

  const statuses = await twoTabsReloadingTogether(context);

  expect(statuses.every((s) => s === 200), `refreshes: ${statuses.join(',')}`).toBeTruthy();
  await context.close();
});

test('logout in one tab sends the other tab to /login', async ({ context }) => {
  const first = await context.newPage();
  await loginUi(first);
  await first.goto('/disciplinary/cases');
  const second = await context.newPage();
  await second.goto('/disciplinary/cases');
  await expectLoggedIn(second);

  await first.getByRole('button', { name: 'Sair' }).click();

  await expect(first).toHaveURL(/\/login$/);
  await expect(second).toHaveURL(/\/login$/);
});
