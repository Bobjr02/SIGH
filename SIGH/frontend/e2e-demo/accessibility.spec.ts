import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';
import { login, profile, settle } from './support/demo';
import { openDetail, DETAILS } from './support/pages';

// axe-core (WCAG 2.0/2.1 A e AA) nas telas principais, nos temas escuro e claro.
// Critério de aceite: NENHUMA violação de contraste (color-contrast). As demais violações também
// falham o teste (sem exceções cadastradas).

const SCREENS: { name: string; who: string; path?: string; detail?: string }[] = [
  { name: 'painel', who: 'juridico', path: '/painel' },
  { name: 'medidas', who: 'juridico', path: '/medidas' },
  { name: 'detalhe de medida', who: 'juridico', detail: 'detalhe de medida' },
  { name: 'funcionários', who: 'juridico', path: '/employees' },
  { name: 'detalhe de funcionário', who: 'juridico', detail: 'detalhe de funcionário' },
  { name: 'relatórios', who: 'juridico', path: '/relatorios' },
  { name: 'auditoria', who: 'juridico', path: '/audit' },
  { name: 'registrar medida', who: 'auxiliar', path: '/medidas/registrar' },
  { name: 'aprovações', who: 'encarregado', path: '/medidas/aprovacoes' },
  { name: 'usuários', who: 'admin', path: '/admin/usuarios' },
  { name: 'detalhe de usuário', who: 'admin', detail: 'detalhe de usuário' },
];

const audit = async (page: Page) => {
  const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze();
  return result.violations.map((v) => ({
    id: v.id,
    impact: v.impact,
    nodes: v.nodes.slice(0, 5).map((n) => `${n.target.join(' ')} — ${n.failureSummary?.split('\n')[1]?.trim() ?? ''}`),
  }));
};

for (const theme of ['dark', 'light'] as const) {
  test(`login (${theme}): sem violações`, async ({ page }) => {
    await page.addInitScript((t) => window.localStorage.setItem('sigh.theme', t), theme);
    await page.goto('/login');
    await settle(page);
    expect(await audit(page)).toEqual([]);
  });

  for (const screen of SCREENS) {
    test(`${screen.name} (${theme}): sem violações`, async ({ page }) => {
      await login(page, profile(screen.who), theme);
      if (screen.detail) {
        expect(await openDetail(page, DETAILS.find((d) => d.name === screen.detail)!)).toBe(true);
      } else {
        await page.goto(screen.path!);
      }
      await settle(page);
      expect(await audit(page)).toEqual([]);
    });
  }
}
