import { expect, type Page } from '@playwright/test';
import enumValues from '../../src/i18n/enum-values.json' with { type: 'json' };

export const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@sigh.com.br';

export interface DemoProfile {
  login: string;
  email: string;
  password: () => string;
  label: string;
}

const demoPassword = () => process.env.SIGH_DEMO_PASSWORD!;

/** Perfis de demonstração (DemoDataSeeder) + o administrador do seed. */
export const PROFILES: DemoProfile[] = [
  { login: 'admin', email: ADMIN_EMAIL, password: () => process.env.E2E_NEW_PASSWORD!, label: 'Administrador' },
  ...['auxiliar', 'encarregado', 'juridico', 'gestor.industrial', 'gestor.agricola', 'preposto'].map((login) => ({
    login,
    email: `${login}@demo.local`,
    password: demoPassword,
    label: login,
  })),
];

export const profile = (login: string) => PROFILES.find((p) => p.login === login)!;

export const login = async (page: Page, who: DemoProfile, theme: 'dark' | 'light' = 'dark') => {
  await page.addInitScript((t) => window.localStorage.setItem('sigh.theme', t), theme);
  await page.goto('/login');
  await page.getByLabel('E-mail').fill(who.email);
  await page.getByLabel('Senha', { exact: true }).fill(who.password());
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByTestId('current-user-name').or(page.getByTestId('dashboard')).first()).toBeVisible({ timeout: 30_000 });
};

/** Espera a página assentar (sem esqueletos de carregamento). */
export const settle = async (page: Page) => {
  await page.waitForLoadState('networkidle');
  await expect(page.locator('[aria-busy="true"]')).toHaveCount(0, { timeout: 20_000 });
  await page.waitForTimeout(300);
};

/**
 * Termos proibidos na tela: os NOMES de todos os valores de enum (enum-values.json, gerado do backend)
 * e palavras comuns de interface em inglês. Exceções: valores cujo nome também é texto válido em
 * português ou é a marca ("Sigh" = SIGH, "Normal", "Legal").
 */
const PORTUGUESE_TOO = new Set(['Sigh', 'Normal', 'Legal']);
export const FORBIDDEN_TERMS: string[] = Array.from(
  new Set([
    ...Object.values(enumValues).flatMap((group) => Object.keys(group)),
    'Pending',
    'Approved',
    'Rejected',
    'Warning',
    'Suspension',
    'Completed',
    'Cancelled',
    'Draft',
    'Loading',
    'Submit',
    'Cancel',
    'Save',
    'Delete',
    'Search',
    'Error',
    'Not found',
    'Unauthorized',
    'Forbidden',
    'Dashboard',
  ]),
).filter((t) => !PORTUGUESE_TOO.has(t));

const escape = (s: string) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

/** Termos encontrados no texto (palavra inteira, sem diferenciar maiúsculas). */
export const forbiddenIn = (text: string): string[] =>
  FORBIDDEN_TERMS.filter((term) => new RegExp(`(?<![\\p{L}\\p{N}_])${escape(term)}(?![\\p{L}\\p{N}_])`, 'iu').test(text));
