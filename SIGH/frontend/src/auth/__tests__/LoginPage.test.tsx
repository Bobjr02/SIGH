import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/server';
import { API, makeUser, meReturns, ok, problem, refreshFails, renderApp } from '../../test/auth';
import { tokenStore } from '../tokenStore';

const fillAndSubmit = async (email: string, password: string) => {
  const user = userEvent.setup();
  await user.type(await screen.findByLabelText(/e-mail/i), email);
  await user.type(screen.getByLabelText(/senha/i), password);
  await user.click(screen.getByRole('button', { name: /entrar/i }));
};

describe('LoginPage', () => {
  it('logs in, loads the user from /auth/me with the new token and opens the requested page', async () => {
    let loginBody: unknown;
    server.use(
      refreshFails(),
      http.post(`${API}/auth/login`, async ({ request }) => {
        loginBody = await request.json();
        return ok({ accessToken: 'login-token', expiresInMinutes: 60, mustChangePassword: false });
      }),
      meReturns(makeUser({ fullName: 'Maria Gestora' }), 'login-token'),
    );

    renderApp('/');

    // Sem sessão: vai para o login.
    expect(await screen.findByRole('heading', { name: 'Entrar no SIGH' })).toBeInTheDocument();

    await fillAndSubmit('maria@sigh.com.br', 'Senha@123');

    expect(await screen.findByTestId('current-user-name')).toHaveTextContent('Maria Gestora');
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/$/);
    expect(loginBody).toEqual({ email: 'maria@sigh.com.br', password: 'Senha@123' });
    expect(tokenStore.get()).toBe('login-token');
    // Nada de tokens ou permissões em localStorage.
    expect(localStorage.length).toBe(0);
  });

  it('shows the API message for invalid credentials and stays on the login page', async () => {
    server.use(refreshFails(), http.post(`${API}/auth/login`, () => problem(400, 'E-mail ou senha inválidos.')));

    renderApp('/login');
    await fillAndSubmit('maria@sigh.com.br', 'errada');

    expect(await screen.findByRole('alert')).toHaveTextContent('E-mail ou senha inválidos.');
    expect(screen.getByTestId('location')).toHaveTextContent('/login');
    expect(tokenStore.get()).toBeNull();
  });

  it('shows the lockout message returned by the API', async () => {
    const lockout = 'Conta temporariamente bloqueada devido a múltiplas tentativas incorretas. Tente novamente mais tarde.';
    server.use(refreshFails(), http.post(`${API}/auth/login`, () => problem(400, lockout)));

    renderApp('/login');
    await fillAndSubmit('maria@sigh.com.br', 'errada');

    expect(await screen.findByRole('alert')).toHaveTextContent(lockout);
  });

  it('shows a rate-limit message on 429 (the API sends no body)', async () => {
    server.use(refreshFails(), http.post(`${API}/auth/login`, () => new HttpResponse(null, { status: 429 })));

    renderApp('/login');
    await fillAndSubmit('maria@sigh.com.br', 'Senha@123');

    expect(await screen.findByRole('alert')).toHaveTextContent(/muitas tentativas/i);
  });

  it('validates the form with zod before calling the API', async () => {
    let loginCalls = 0;
    server.use(
      refreshFails(),
      http.post(`${API}/auth/login`, () => {
        loginCalls += 1;
        return ok({});
      }),
    );

    renderApp('/login');
    const user = userEvent.setup();
    await user.type(await screen.findByLabelText(/e-mail/i), 'nao-e-email');
    await user.click(screen.getByRole('button', { name: /entrar/i }));

    expect(await screen.findByText('Informe um e-mail válido.')).toBeInTheDocument();
    expect(screen.getByText('Informe a senha.')).toBeInTheDocument();
    expect(loginCalls).toBe(0);
  });

  it('sends the user to /change-password when the account must change the password', async () => {
    server.use(
      refreshFails(),
      http.post(`${API}/auth/login`, () => ok({ accessToken: 't', expiresInMinutes: 60, mustChangePassword: true })),
      meReturns(makeUser({ mustChangePassword: true })),
    );

    renderApp('/login');
    await fillAndSubmit('admin@sigh.com.br', 'Inicial@123');

    expect(await screen.findByRole('heading', { name: 'Trocar senha' })).toBeInTheDocument();
    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/change-password'));
  });
});
