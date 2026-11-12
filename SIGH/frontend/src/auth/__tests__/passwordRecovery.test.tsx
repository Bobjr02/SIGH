import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/server';
import { API, ok, problem, refreshFails, renderApp } from '../../test/auth';

const GENERIC = 'Se o e-mail informado estiver cadastrado, você receberá as instruções para redefinir a senha.';

describe('forgot password', () => {
  it('the login page links to /forgot-password', async () => {
    server.use(refreshFails());
    renderApp('/login');

    await userEvent.setup().click(await screen.findByRole('link', { name: 'Esqueci minha senha' }));

    expect(await screen.findByRole('heading', { name: 'Esqueci minha senha' })).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/forgot-password');
  });

  it('sends the e-mail and shows the generic message from the API', async () => {
    let body: unknown;
    server.use(
      refreshFails(),
      http.post(`${API}/auth/forgot-password`, async ({ request }) => {
        body = await request.json();
        return ok({ success: true, message: GENERIC });
      }),
    );
    renderApp('/forgot-password');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText(/e-mail/i), 'maria@sigh.com.br');
    await user.click(screen.getByRole('button', { name: 'Enviar link' }));

    expect(await screen.findByText(GENERIC)).toBeInTheDocument();
    expect(body).toEqual({ email: 'maria@sigh.com.br' });
    expect(screen.queryByRole('button', { name: 'Enviar link' })).not.toBeInTheDocument();
  });

  it('validates the e-mail with zod before calling the API', async () => {
    let calls = 0;
    server.use(
      refreshFails(),
      http.post(`${API}/auth/forgot-password`, () => {
        calls += 1;
        return ok({});
      }),
    );
    renderApp('/forgot-password');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText(/e-mail/i), 'nao-e-email');
    await user.click(screen.getByRole('button', { name: 'Enviar link' }));

    expect(await screen.findByText('Informe um e-mail válido.')).toBeInTheDocument();
    expect(calls).toBe(0);
  });

  it('shows the rate-limit message on 429', async () => {
    server.use(refreshFails(), http.post(`${API}/auth/forgot-password`, () => new HttpResponse(null, { status: 429 })));
    renderApp('/forgot-password');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText(/e-mail/i), 'maria@sigh.com.br');
    await user.click(screen.getByRole('button', { name: 'Enviar link' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(/muitas tentativas/i);
  });
});

describe('reset password', () => {
  it('resets with the token from the link', async () => {
    let body: unknown;
    server.use(
      refreshFails(),
      http.post(`${API}/auth/reset-password`, async ({ request }) => {
        body = await request.json();
        return ok({ success: true, message: 'ok' });
      }),
    );
    renderApp('/reset-password?token=tok%2Babc123');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText('Nova senha'), 'Nova@Senha2026!');
    await user.type(screen.getByLabelText('Confirmar nova senha'), 'Nova@Senha2026!');
    await user.click(screen.getByRole('button', { name: 'Salvar senha' }));

    expect(await screen.findByText(/senha redefinida com sucesso/i)).toBeInTheDocument();
    expect(body).toEqual({ token: 'tok+abc123', newPassword: 'Nova@Senha2026!' });
    expect(screen.getByRole('link', { name: 'Ir para o login' })).toHaveAttribute('href', '/login');
  });

  it('first access (setup=1) says "Definir senha"', async () => {
    server.use(refreshFails());
    renderApp('/reset-password?token=abc&setup=1');

    expect(await screen.findByRole('heading', { name: 'Definir senha' })).toBeInTheDocument();
  });

  it('shows the API error for an expired link and offers a new one', async () => {
    server.use(
      refreshFails(),
      http.post(`${API}/auth/reset-password`, () => problem(400, 'Link de redefinição inválido ou expirado. Solicite um novo.')),
    );
    renderApp('/reset-password?token=expirado');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText('Nova senha'), 'Nova@Senha2026!');
    await user.type(screen.getByLabelText('Confirmar nova senha'), 'Nova@Senha2026!');
    await user.click(screen.getByRole('button', { name: 'Salvar senha' }));

    expect(await screen.findByText(/inválido ou expirado/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Pedir um novo link' })).toHaveAttribute('href', '/forgot-password');
  });

  it('rejects a confirmation that does not match before calling the API', async () => {
    let calls = 0;
    server.use(
      refreshFails(),
      http.post(`${API}/auth/reset-password`, () => {
        calls += 1;
        return ok({});
      }),
    );
    renderApp('/reset-password?token=abc');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText('Nova senha'), 'Nova@Senha2026!');
    await user.type(screen.getByLabelText('Confirmar nova senha'), 'Outra@Senha2026!');
    await user.click(screen.getByRole('button', { name: 'Salvar senha' }));

    expect(await screen.findByText('A confirmação não confere com a nova senha.')).toBeInTheDocument();
    await waitFor(() => expect(calls).toBe(0));
  });

  it('without a token shows an invalid-link message and no form', async () => {
    server.use(refreshFails());
    renderApp('/reset-password');

    expect(await screen.findByText(/link inválido/i)).toBeInTheDocument();
    expect(screen.queryByLabelText('Nova senha')).not.toBeInTheDocument();
  });
});
