import { expect, type APIRequestContext, type Page } from '@playwright/test';

export const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@sigh.com.br';
/** Senha do admin depois da troca obrigatória (auth-flow.spec). */
export const adminPassword = () => {
  const value = process.env.E2E_NEW_PASSWORD;
  if (!value) throw new Error('E2E_NEW_PASSWORD não definida.');
  return value;
};

const MAILPIT = process.env.E2E_MAILPIT_URL ?? 'http://localhost:8025';

export const loginUi = async (page: Page, email = ADMIN_EMAIL, password = adminPassword()) => {
  await page.goto('/login');
  await page.getByLabel('E-mail').fill(email);
  await page.getByLabel('Senha').fill(password);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await expect(page.getByTestId('current-user-name')).toBeVisible();
};

/** Access token por API (sessão própria, separada do navegador). */
export const apiToken = async (request: APIRequestContext, email = ADMIN_EMAIL, password = adminPassword()) => {
  const response = await request.post('/api/v1/auth/login', { data: { email, password } });
  expect(response.ok(), await response.text()).toBeTruthy();
  return (await response.json()).data.accessToken as string;
};

export const authed = (token: string) => ({ Authorization: `Bearer ${token}` });

export interface MailpitMessage {
  ID: string;
  Subject: string;
  To: { Address: string }[];
}

/** Espera chegar ao Mailpit um e-mail para o destinatário e devolve o texto. */
export const waitForEmail = async (request: APIRequestContext, to: string, subjectIncludes: string, timeoutMs = 30_000) => {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const search = await request.get(`${MAILPIT}/api/v1/search`, { params: { query: `to:"${to}"` } });
    if (search.ok()) {
      const messages: MailpitMessage[] = (await search.json()).messages ?? [];
      const hit = messages.find((m) => m.Subject.includes(subjectIncludes));
      if (hit) {
        const message = await (await request.get(`${MAILPIT}/api/v1/message/${hit.ID}`)).json();
        return { subject: hit.Subject, text: message.Text as string, html: message.HTML as string };
      }
    }
    await new Promise((r) => setTimeout(r, 1000));
  }
  throw new Error(`Nenhum e-mail "${subjectIncludes}" para ${to} chegou ao Mailpit em ${timeoutMs} ms.`);
};

/** PDF pequeno, mas válido. */
export const pdfBytes = () =>
  Buffer.from(
    '%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n' +
      '3 0 obj<</Type/Page/Parent 2 0 R/MediaBox[0 0 200 200]>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n',
    'latin1',
  );

/** Cabeçalho de executável Windows (MZ...PE). */
export const exeBytes = () => Buffer.from([0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0xff, 0xff, 0x00, 0x00, 0xb8, 0x00, 0x50, 0x45]);
