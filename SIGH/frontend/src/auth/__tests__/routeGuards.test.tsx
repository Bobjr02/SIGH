import { describe, expect, it } from 'vitest';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http } from 'msw';
import { Route, Routes } from 'react-router-dom';
import { server } from '../../test/server';
import { API, makeUser, meReturns, ok, refreshFails, refreshSucceeds, renderApp } from '../../test/auth';
import { ProtectedRoute } from '../ProtectedRoute';
import { RequirePermission } from '../RequirePermission';
import { PERMISSIONS } from '../permissions';
import { tokenStore } from '../tokenStore';

const GuardedRoutes = () => (
  <Routes>
    <Route path="/login" element={<h1>Tela de login</h1>} />
    <Route element={<ProtectedRoute />}>
      <Route path="/secret" element={<h1>Área protegida</h1>} />
      <Route
        path="/cases"
        element={
          <RequirePermission permission={PERMISSIONS.DISCIPLINARY_CASES_VIEW}>
            <h1>Lista de processos</h1>
          </RequirePermission>
        }
      />
      <Route
        path="/reports"
        element={
          <RequirePermission anyOf={[PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_CASES, PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES]}>
            <h1>Relatórios</h1>
          </RequirePermission>
        }
      />
    </Route>
  </Routes>
);

describe('ProtectedRoute', () => {
  it('redirects to /login without a session', async () => {
    server.use(refreshFails());

    renderApp('/secret', <GuardedRoutes />);

    expect(await screen.findByRole('heading', { name: 'Tela de login' })).toBeInTheDocument();
    expect(screen.queryByText('Área protegida')).not.toBeInTheDocument();
  });

  it('renders the page with a session', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser()));

    const { visited } = renderApp('/secret', <GuardedRoutes />);

    expect(await screen.findByRole('heading', { name: 'Área protegida' })).toBeInTheDocument();
    expect(visited).not.toContain('/login');
  });
});

describe('RequirePermission', () => {
  it('shows the 403 page when the user lacks the permission', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.NOTIFICATIONS_READ] })));

    renderApp('/cases', <GuardedRoutes />);

    expect(await screen.findByRole('heading', { name: /403/ })).toBeInTheDocument();
    expect(screen.queryByText('Lista de processos')).not.toBeInTheDocument();
  });

  it('renders the content when the user has the permission', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_CASES_VIEW] })));

    renderApp('/cases', <GuardedRoutes />);

    expect(await screen.findByRole('heading', { name: 'Lista de processos' })).toBeInTheDocument();
  });

  it('with anyOf, one of the permissions is enough', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES] })));

    renderApp('/reports', <GuardedRoutes />);

    expect(await screen.findByRole('heading', { name: 'Relatórios' })).toBeInTheDocument();
  });

  it('does not accept legacy ".Read" codes as ".View" (no View/Read mapping)', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: ['Disciplinary.Cases.Read'] })));

    renderApp('/cases', <GuardedRoutes />);

    expect(await screen.findByRole('heading', { name: /403/ })).toBeInTheDocument();
  });

  it('protects the real app routes: /disciplinary/infraction-types without permission shows 403', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [] })));

    renderApp('/disciplinary/infraction-types');

    expect(await screen.findByRole('heading', { name: /403/ })).toBeInTheDocument();
  });
});

describe('header', () => {
  it('shows only the menu items the user is allowed to see', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(
        makeUser({
          permissions: [PERMISSIONS.DISCIPLINARY_CASES_VIEW, PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_DASHBOARD],
        }),
      ),
    );

    renderApp('/');

    const nav = await screen.findByRole('navigation', { name: 'Menu principal' });
    expect(within(nav).getByRole('button', { name: /processos/i })).toBeInTheDocument();
    expect(within(nav).getByRole('button', { name: 'Dashboard' })).toBeInTheDocument();
    expect(within(nav).queryByRole('button', { name: 'Relatórios' })).not.toBeInTheDocument();
    expect(within(nav).queryByRole('button', { name: 'Medidas' })).not.toBeInTheDocument();
    expect(within(nav).queryByRole('button', { name: 'Histórico' })).not.toBeInTheDocument();
    expect(within(nav).queryByRole('button', { name: /tipos de infração/i })).not.toBeInTheDocument();
    expect(within(nav).queryByRole('button', { name: /novo processo/i })).not.toBeInTheDocument();
  });

  it('logout revokes the session on the API, clears the token and goes to /login', async () => {
    let logoutCalls = 0;
    server.use(
      refreshSucceeds('session-token'),
      meReturns(makeUser({ fullName: 'Maria Gestora' })),
      http.post(`${API}/auth/logout`, ({ request }) => {
        logoutCalls += 1;
        expect(request.headers.get('Authorization')).toBe('Bearer session-token');
        return ok({ success: true });
      }),
    );

    renderApp('/');
    expect(await screen.findByTestId('current-user-name')).toHaveTextContent('Maria Gestora');

    await userEvent.setup().click(screen.getByRole('button', { name: 'Sair' }));

    expect(await screen.findByRole('heading', { name: 'Entrar no SIGH' })).toBeInTheDocument();
    expect(logoutCalls).toBe(1);
    expect(tokenStore.get()).toBeNull();
  });
});
