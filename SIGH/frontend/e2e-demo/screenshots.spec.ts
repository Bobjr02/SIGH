import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { test, type Page } from '@playwright/test';
import { login, profile, settle } from './support/demo';
import { DETAILS, openDetail } from './support/pages';

// Capturas para o dono do produto (SIGH/docs/telas), deviceScaleFactor 1, temas escuro e claro;
// login e painel também em largura de celular (390 px). Só roda com SIGH_SCREENSHOTS=1.
const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT = path.resolve(HERE, '../../docs/telas');

test.skip(process.env.SIGH_SCREENSHOTS !== '1', 'capturas só com SIGH_SCREENSHOTS=1');

const shot = (page: Page, file: string) => page.screenshot({ path: path.join(OUT, `${file}.png`) });

const SCREENS: { file: string; who: string; path?: string; detail?: string; prepare?: (page: Page) => Promise<void> }[] = [
  { file: '02-painel', who: 'juridico', path: '/painel' },
  { file: '03-registrar-medida', who: 'auxiliar', path: '/medidas/registrar' },
  { file: '04-aprovacoes', who: 'encarregado', path: '/medidas/aprovacoes' },
  { file: '05-medidas', who: 'juridico', path: '/medidas' },
  // Medida aprovada no SIGH: mostra "Aplicado por".
  { file: '06-detalhe-de-medida', who: 'juridico', path: '/medidas', detail: 'approved' },
  { file: '07-funcionarios', who: 'juridico', path: '/employees' },
  { file: '08-detalhe-de-funcionario', who: 'juridico', detail: 'detalhe de funcionário' },
  { file: '09-relatorios', who: 'juridico', path: '/relatorios' },
  { file: '10-auditoria', who: 'juridico', path: '/audit' },
  { file: '11-usuarios', who: 'admin', path: '/admin/usuarios' },
  { file: '12-detalhe-de-usuario', who: 'admin', detail: 'detalhe de usuário' },
];

for (const theme of ['escuro', 'claro'] as const) {
  const mode = theme === 'escuro' ? 'dark' : 'light';

  test(`login (${theme})`, async ({ page }) => {
    await page.addInitScript((t) => window.localStorage.setItem('sigh.theme', t), mode);
    await page.goto('/login');
    await settle(page);
    await shot(page, `01-login-${theme}`);
    await page.setViewportSize({ width: 390, height: 844 });
    await settle(page);
    await shot(page, `01-login-${theme}-celular`);
  });

  for (const screen of SCREENS) {
    test(`${screen.file} (${theme})`, async ({ page }) => {
      await login(page, profile(screen.who), mode);
      if (screen.detail === 'approved') {
        await page.goto('/medidas');
        await settle(page);
        await page.getByRole('combobox', { name: 'Situação' }).click();
        await page.getByRole('option', { name: 'Aprovada' }).click();
        await settle(page);
        await page.locator('[data-testid="record-row"] a[href^="/medidas/"]').first().click();
      } else if (screen.detail) {
        await openDetail(page, DETAILS.find((d) => d.name === screen.detail)!);
      } else {
        await page.goto(screen.path!);
      }
      await settle(page);
      await shot(page, `${screen.file}-${theme}`);
      if (screen.file === '02-painel') {
        await page.setViewportSize({ width: 390, height: 844 });
        await settle(page);
        await shot(page, `${screen.file}-${theme}-celular`);
      }
    });
  }
}
