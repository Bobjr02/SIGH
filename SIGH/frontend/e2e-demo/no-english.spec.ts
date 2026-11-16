import { expect, test, type Page } from '@playwright/test';
import { syntheticCpf } from '../e2e/support/xlsx';
import { FORBIDDEN_TERMS, forbiddenIn, login, profile, PROFILES, settle } from './support/demo';
import { DETAILS, menuPaths, openDetail, screenText, visibleTextWithTabs } from './support/pages';

// Nenhum termo em inglês no texto visível E nos atributos acessíveis (aria-label, title, alt,
// placeholder), em todas as páginas do menu de cada perfil, nos detalhes (com todas as abas), nas
// notificações, na troca de senha e no login; e também em alertas, diálogos, paginação e no
// Autocomplete (textos padrão do MUI). Palavra inteira, sem diferenciar maiúsculas. A lista inclui o
// nome de TODOS os valores de enum (enum-values.json) e os textos padrão do MUI em inglês.

/** Falha com a lista do que achou (onde: "termo"). */
const expectNoEnglish = async (page: Page, where: string, found: string[] = []) => {
  for (const term of forbiddenIn(await screenText(page))) found.push(`${where}: "${term}"`);
  return found;
};

test('the forbidden list covers every enum value and the common English UI words', () => {
  expect(FORBIDDEN_TERMS).toEqual(expect.arrayContaining(['PendingApproval', 'JustCauseTermination', 'PendingFirstAccess', 'Completed', 'Not found', 'Loading']));
  expect(forbiddenIn('Situação: Aprovada')).toEqual([]);
  expect(forbiddenIn('status: Completed')).toEqual(['Completed']);
  expect(forbiddenIn('page not found')).toEqual(['Not found']);
  expect(forbiddenIn('SIGH v0.0.0')).toEqual([]);
  // Textos padrão do MUI sem localização.
  expect(FORBIDDEN_TERMS).toEqual(expect.arrayContaining(['Close', 'Rows per page', 'of', 'Open', 'Clear', 'Previous', 'Next', 'Go to page']));
  expect(forbiddenIn('Rows per page: 10')).toEqual(['Rows per page']);
  expect(forbiddenIn('1–10 of 248')).toEqual(['of']);
  expect(forbiddenIn('Go to page 2')).toEqual(['Go to page']);
  expect(forbiddenIn('Linhas por página: 10 · 1–10 de 248 · Fechar · Abrir · Limpar · Ir para a próxima página')).toEqual([]);
  // "of"/"Open" só como palavra inteira: "ofício", "Openheimer" não contam.
  expect(forbiddenIn('Ofício enviado ao Openheimer')).toEqual([]);
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

test('admin: new user dialog, the "user created" alert and the paginated list have no English', async ({ page }) => {
  const found: string[] = [];
  await login(page, profile('admin'));

  // Lista de usuários (paginação) e o diálogo "Novo usuário".
  await page.goto('/admin/usuarios');
  await settle(page);
  await expectNoEnglish(page, 'lista de usuários', found);
  await page.getByRole('button', { name: 'Novo usuário' }).click();
  const dialog = page.getByRole('dialog', { name: 'Novo usuário' });
  await expect(dialog).toBeVisible();
  await expectNoEnglish(page, 'diálogo Novo usuário', found);

  // Cria um usuário FICTÍCIO: o detalhe abre com o alerta "Usuário criado..." e o botão de fechar.
  const stamp = Date.now();
  await dialog.getByLabel('Nome completo').fill('Pessoa Fictícia TESTE');
  await dialog.getByLabel('E-mail').fill(`ficticia.${stamp}@demo.local`);
  await dialog.getByLabel('CPF').fill(syntheticCpf(String(stamp).slice(-9)));
  await dialog.getByRole('checkbox', { name: 'RH – Registro' }).check();
  await dialog.getByRole('button', { name: 'Criar usuário' }).click();
  const alert = page.getByRole('alert').filter({ hasText: 'Usuário criado' });
  await expect(alert).toBeVisible();
  await expect(alert.getByRole('button', { name: 'Fechar' })).toBeVisible();
  await expectNoEnglish(page, 'alerta Usuário criado', found);

  // Paginação de uma lista grande (funcionários de demonstração): página seguinte inclusive.
  await page.goto('/employees');
  await settle(page);
  await expectNoEnglish(page, 'funcionários, página 1', found);
  const next = page.getByRole('button', { name: 'Próxima página' });
  if (await next.isEnabled()) {
    await next.click();
    await settle(page);
    await expectNoEnglish(page, 'funcionários, página 2', found);
  }

  expect(found, 'termos em inglês').toEqual([]);
});

test('auxiliar: the employee Autocomplete (open, clear, no options) has no English', async ({ page }) => {
  const found: string[] = [];
  await login(page, profile('auxiliar'));
  await page.goto('/medidas/registrar');
  await settle(page);
  const form = page.getByRole('form', { name: 'Registrar medida' });
  const employee = form.getByRole('combobox', { name: 'Funcionário', exact: true });

  // Botões "Abrir"/"Fechar"/"Limpar" do Autocomplete e a lista sem resultados.
  await employee.fill('zzzz nenhum funcionário');
  await expect(page.getByText('Nenhum funcionário encontrado')).toBeVisible();
  await expectNoEnglish(page, 'Autocomplete sem resultados', found);
  await employee.fill('');
  await employee.click();
  await expectNoEnglish(page, 'Autocomplete aberto', found);

  expect(found, 'termos em inglês').toEqual([]);
});

test('encarregado: the reject dialog has no English', async ({ page }) => {
  const found: string[] = [];
  await login(page, profile('encarregado'));
  await page.goto('/medidas/aprovacoes');
  await settle(page);
  const row = page.getByTestId('record-row').first();
  test.skip((await row.count()) === 0, 'sem medidas aguardando aprovação nos dados de demonstração');
  await row.getByRole('link', { name: 'Ver medida' }).click();
  await page.getByRole('button', { name: 'Rejeitar' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expectNoEnglish(page, 'diálogo Rejeitar medida', found);
  await page.keyboard.press('Escape');

  expect(found, 'termos em inglês').toEqual([]);
});
