import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { expect, type Page } from '@playwright/test';
import { settle } from './demo';

// Arquivos FICTÍCIOS do pacote de referência, usados para mostrar o seletor com um arquivo escolhido.
const HERE = path.dirname(fileURLToPath(import.meta.url));
const REFS = path.resolve(HERE, '../../../docs/referencias');
export const EMPLOYEES_FILE = path.join(REFS, 'importacao-funcionarios', 'funcionarios_carga_inicial.xlsx');
export const HISTORY_FILE = path.join(REFS, 'historico-disciplinar', 'base_disciplinar_ficticia.xlsx');

/** Abre uma medida aprovada (aceita anexos) na aba Anexos, com o seletor de arquivo. */
export const attachableRecord = async (page: Page) => {
  await page.goto('/medidas');
  await settle(page);
  await page.getByRole('combobox', { name: 'Situação' }).click();
  await page.getByRole('option', { name: 'Aprovada' }).click();
  await settle(page);
  await page.locator('[data-testid="record-row"] a[href^="/medidas/"]').first().click();
  await settle(page);
  await page.getByRole('tab', { name: /^Anexos/ }).click();
  const attachments = page.getByRole('region', { name: 'Anexos' });
  await expect(attachments.getByTestId('file-picker')).toBeVisible();
  await attachments.scrollIntoViewIfNeeded();
};
