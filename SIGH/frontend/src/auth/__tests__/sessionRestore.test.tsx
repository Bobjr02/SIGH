import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { delay, http } from 'msw';
import { server } from '../../test/server';
import {
  API,
  makeUser,
  meReturns,
  problem,
  refreshFailResponse,
  refreshFails,
  refreshOkResponse,
  renderApp,
} from '../../test/auth';
import { tokenStore } from '../tokenStore';

describe('session restore on app load', () => {
  it('restores the session with POST /auth/refresh + GET /auth/me, showing a loader and never the login page', async () => {
    let refreshCalls = 0;
    server.use(
      http.post(`${API}/auth/refresh`, async () => {
        refreshCalls += 1;
        await delay(30);
        return refreshOkResponse('restored-token');
      }),
      meReturns(makeUser({ fullName: 'Maria Gestora' }), 'restored-token'),
    );

    const { visited } = renderApp('/');

    expect(screen.getByRole('status')).toHaveTextContent('Carregando sessão...');
    expect(await screen.findByTestId('current-user-name')).toHaveTextContent('Maria Gestora');

    expect(visited).not.toContain('/login');
    expect(screen.queryByRole('heading', { name: 'Entrar no SIGH' })).not.toBeInTheDocument();
    expect(refreshCalls).toBe(1);
    expect(tokenStore.get()).toBe('restored-token');
  });

  it('goes to /login when there is no valid refresh cookie', async () => {
    server.use(refreshFails());

    renderApp('/disciplinary/cases');

    expect(await screen.findByRole('heading', { name: 'Entrar no SIGH' })).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/login');
    expect(tokenStore.get()).toBeNull();
  });

  it('goes to /login when the refresh works but /auth/me fails', async () => {
    let refreshCalls = 0;
    server.use(
      http.post(`${API}/auth/refresh`, () => {
        refreshCalls += 1;
        // primeira chamada (restauração) funciona; o retry do interceptor após o 401 do /me falha
        return refreshCalls === 1 ? refreshOkResponse('t1') : refreshFailResponse();
      }),
      http.get(`${API}/auth/me`, () => problem(401, 'Usuário não encontrado.')),
    );

    renderApp('/');

    expect(await screen.findByRole('heading', { name: 'Entrar no SIGH' })).toBeInTheDocument();
    await waitFor(() => expect(tokenStore.get()).toBeNull());
  });
});
