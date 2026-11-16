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

/** Atributos lidos por leitores de tela ou mostrados ao passar o mouse (inclui botões só com ícone). */
const ACCESSIBLE_ATTRIBUTES = ['aria-label', 'title', 'alt', 'placeholder', 'aria-valuetext', 'aria-roledescription'];

/**
 * Texto que a pessoa vê OU ouve: o texto visível do body mais os atributos acessíveis de todos os
 * elementos do documento (também os de portais: diálogos, menus, listas do Autocomplete, tooltips).
 * Ex.: o botão de fechar do Alert do MUI não tem texto, só aria-label/title ("Close" sem o ptBR).
 */
export const screenText = async (page: Page): Promise<string> => {
  const visible = await page.locator('body').innerText();
  const attributes = await page.evaluate((names) => {
    const values: string[] = [];
    for (const el of Array.from(document.querySelectorAll('*'))) {
      for (const name of names) {
        const value = el.getAttribute(name);
        if (value) values.push(value);
      }
    }
    return values;
  }, ACCESSIBLE_ATTRIBUTES);
  return `${visible}\n${attributes.join('\n')}`;
};

/** Abre cada aba visível da página e devolve o texto (visível + acessível) de todas. */
export const visibleTextWithTabs = async (page: Page): Promise<string> => {
  await settle(page);
  let text = await screenText(page);
  const tabs = page.getByRole('tab');
  const count = await tabs.count();
  for (let i = 0; i < count; i++) {
    await tabs.nth(i).click();
    await settle(page);
    text += '\n' + (await screenText(page));
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
