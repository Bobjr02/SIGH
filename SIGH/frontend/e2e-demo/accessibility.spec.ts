import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';
import { login, profile, settle } from './support/demo';
import { openDetail, DETAILS } from './support/pages';
import { attachableRecord, EMPLOYEES_FILE, HISTORY_FILE } from './support/files';

// axe-core (WCAG 2.0/2.1 A e AA) nas telas principais, nos temas escuro e claro.
// Critério de aceite: NENHUMA violação de contraste (color-contrast). As demais violações também
// falham o teste (sem exceções cadastradas).

const SCREENS: { name: string; who: string; path?: string; detail?: string; prepare?: (page: Page) => Promise<void> }[] = [
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
  // Telas com o seletor de arquivo (FilePicker): vazio, com arquivo escolhido e com extensão errada.
  { name: 'importar funcionários', who: 'admin', path: '/employees/import' },
  {
    name: 'importar funcionários com arquivo',
    who: 'admin',
    path: '/employees/import',
    prepare: (page) => page.getByLabel('Planilha do SAP').setInputFiles(EMPLOYEES_FILE),
  },
  {
    name: 'importar funcionários com extensão errada',
    who: 'admin',
    path: '/employees/import',
    prepare: (page) => page.getByLabel('Planilha do SAP').setInputFiles({ name: 'base.csv', mimeType: 'text/csv', buffer: Buffer.from('a;b') }),
  },
  { name: 'importar histórico', who: 'admin', path: '/admin/history-import' },
  {
    name: 'importar histórico com arquivo',
    who: 'admin',
    path: '/admin/history-import',
    prepare: (page) => page.getByLabel('Planilha de penalidades').setInputFiles(HISTORY_FILE),
  },
  { name: 'anexos da medida', who: 'auxiliar', prepare: attachableRecord },
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
      } else if (screen.path) {
        await page.goto(screen.path);
      }
      await settle(page);
      if (screen.prepare) {
        await screen.prepare(page);
        await settle(page);
      }
      expect(await audit(page)).toEqual([]);
    });
  }
}
