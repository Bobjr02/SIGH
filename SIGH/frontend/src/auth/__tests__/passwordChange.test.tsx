import { describe, expect, it } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { server } from '../../test/server';
import { API, makeUser, meReturns, ok, problem, refreshSucceeds, renderApp } from '../../test/auth';
import { PERMISSIONS } from '../permissions';
import { PASSWORD_CHANGE_REQUIRED } from '../../services/api';
import { tokenStore } from '../tokenStore';

describe('mandatory password change', () => {
  it('redirects to /change-password when the API answers 403 PASSWORD_CHANGE_REQUIRED', async () => {
    // O /me diz que não precisa trocar, mas a API exige (ex.: a claim do token ainda está ativa).
    server.use(
      refreshSucceeds('t1'),
      meReturns(makeUser({ permissions: [PERMISSIONS.NOTIFICATIONS_VIEW] })),
      http.get(`${API}/notifications/unread/count`, () =>
        problem(403, 'Troca de senha obrigatória', { code: PASSWORD_CHANGE_REQUIRED }),
      ),
      http.get(`${API}/notifications/unread`, () =>
        problem(403, 'Troca de senha obrigatória', { code: PASSWORD_CHANGE_REQUIRED }),
      ),
    );

    renderApp('/');

    expect(await screen.findByRole('heading', { name: 'Trocar senha' })).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/change-password');
  });

  it('keeps the user on /change-password while mustChangePassword is true', async () => {
    server.use(refreshSucceeds('t1'), meReturns(makeUser({ mustChangePassword: true })));

    const { visited } = renderApp('/disciplinary/cases');

    expect(await screen.findByRole('heading', { name: 'Trocar senha' })).toBeInTheDocument();
    expect(screen.getByText(/defina uma nova senha antes de continuar/i)).toBeInTheDocument();
    expect(visited).not.toContain('/login');
    // Não há menu nem saída para outras páginas além de "Sair".
    expect(screen.queryByRole('navigation', { name: 'Menu principal' })).not.toBeInTheDocument();
  });

  it('after changing the password uses the new access token and leaves the page', async () => {
    let changeBody: unknown;
    let meCalls = 0;
    server.use(
      refreshSucceeds('old-token'),
      http.get(`${API}/auth/me`, ({ request }) => {
        meCalls += 1;
        const auth = request.headers.get('Authorization');
        if (auth === 'Bearer old-token') return ok(makeUser({ mustChangePassword: true }));
        if (auth === 'Bearer new-token') return ok(makeUser({ mustChangePassword: false, fullName: 'Admin Trocado' }));
        return problem(401, 'Token inválido.');
      }),
      http.post(`${API}/auth/change-password`, async ({ request }) => {
        changeBody = await request.json();
        if (request.headers.get('Authorization') !== 'Bearer old-token') return problem(401, 'x');
        return ok({ success: true, message: 'ok', accessToken: 'new-token', expiresInMinutes: 60 });
      }),
    );

    renderApp('/');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText('Senha atual'), 'Inicial@123');
    await user.type(screen.getByLabelText('Nova senha'), 'Nova@Senha456');
    await user.type(screen.getByLabelText('Confirmar nova senha'), 'Nova@Senha456');
    await user.click(screen.getByRole('button', { name: /salvar nova senha/i }));

    expect(await screen.findByTestId('current-user-name')).toHaveTextContent('Admin Trocado');
    expect(screen.getByTestId('location')).toHaveTextContent(/^\/$/);
    expect(changeBody).toEqual({ currentPassword: 'Inicial@123', newPassword: 'Nova@Senha456' });
    expect(tokenStore.get()).toBe('new-token');
    expect(meCalls).toBeGreaterThanOrEqual(2);
  });

  it('shows the API error (e.g. password policy) and stays on the page', async () => {
    server.use(
      refreshSucceeds('old-token'),
      meReturns(makeUser({ mustChangePassword: true })),
      http.post(`${API}/auth/change-password`, () => problem(400, 'A senha atual informada está incorreta.')),
    );

    renderApp('/change-password');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText('Senha atual'), 'errada');
    await user.type(screen.getByLabelText('Nova senha'), 'Nova@Senha456');
    await user.type(screen.getByLabelText('Confirmar nova senha'), 'Nova@Senha456');
    await user.click(screen.getByRole('button', { name: /salvar nova senha/i }));

    expect(await screen.findByText('A senha atual informada está incorreta.')).toBeInTheDocument();
    expect(screen.getByTestId('location')).toHaveTextContent('/change-password');
  });

  it('rejects a confirmation that does not match before calling the API', async () => {
    let calls = 0;
    server.use(
      refreshSucceeds('t'),
      meReturns(makeUser({ mustChangePassword: true })),
      http.post(`${API}/auth/change-password`, () => {
        calls += 1;
        return ok({});
      }),
    );

    renderApp('/change-password');
    const user = userEvent.setup();

    await user.type(await screen.findByLabelText('Senha atual'), 'Inicial@123');
    await user.type(screen.getByLabelText('Nova senha'), 'Nova@Senha456');
    await user.type(screen.getByLabelText('Confirmar nova senha'), 'Outra@Senha789');
    await user.click(screen.getByRole('button', { name: /salvar nova senha/i }));

    expect(await screen.findByText('A confirmação não confere com a nova senha.')).toBeInTheDocument();
    await waitFor(() => expect(calls).toBe(0));
  });
});
