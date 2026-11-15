import { expect, type Page } from '@playwright/test';
import { settle } from './demo';

export interface Visit {
  /** Nome da página (relatório do teste e das capturas). */
  name: string;
  path: string;
  /** Abre um detalhe a partir da lista (link do primeiro item). */
  openFirst?: string;
}

/** Itens do menu do perfil (só o que ele pode ver) + Notificações e Trocar senha. */
export const menuPaths = async (page: Page): Promise<string[]> => {
  const links = page.getByRole('navigation', { name: 'Menu principal' }).getByRole('link');
  const hrefs = await links.evaluateAll((as) => as.map((a) => (a as HTMLAnchorElement).getAttribute('href') ?? ''));
  return [...new Set(['/', ...hrefs.filter(Boolean)])];
};

/** Detalhes alcançáveis a partir das listas (primeiro item). */
export const DETAILS: Visit[] = [
  { name: 'detalhe de medida', path: '/medidas', openFirst: '[data-testid="record-row"] a[href^="/medidas/"]' },
  { name: 'detalhe de funcionário', path: '/employees', openFirst: 'a[aria-label="Ver funcionário"]' },
  { name: 'detalhe de usuário', path: '/admin/usuarios', openFirst: 'a[aria-label="Ver usuário"]' },
];

/** Abre cada aba visível da página e devolve o texto visível de todas. */
export const visibleTextWithTabs = async (page: Page): Promise<string> => {
  await settle(page);
  let text = await page.locator('body').innerText();
  const tabs = page.getByRole('tab');
  const count = await tabs.count();
  for (let i = 0; i < count; i++) {
    await tabs.nth(i).click();
    await settle(page);
    text += '\n' + (await page.locator('body').innerText());
  }
  return text;
};

export const openDetail = async (page: Page, visit: Visit): Promise<boolean> => {
  await page.goto(visit.path);
  await settle(page);
  const link = page.locator(visit.openFirst!).first();
  if ((await link.count()) === 0) return false;
  await link.click();
  await expect(page.getByRole('navigation', { name: 'Trilha de navegação' })).toBeVisible();
  return true;
};
