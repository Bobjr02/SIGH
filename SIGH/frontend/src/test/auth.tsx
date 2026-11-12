import React, { StrictMode, useEffect } from 'react';
import { render } from '@testing-library/react';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse, delay } from 'msw';
import { AuthProvider } from '../contexts/AuthContext';
import { AppRoutes } from '../routes';
import type { CurrentUser } from '../auth/authApi';
import { PERMISSIONS } from '../auth/permissions';

export const API = '/api/v1';

export const makeUser = (overrides: Partial<CurrentUser> = {}): CurrentUser => ({
  id: '0b6c1d3e-0000-4000-8000-000000000001',
  fullName: 'Maria Gestora',
  email: 'maria@sigh.com.br',
  status: 'Active',
  mustChangePassword: false,
  roles: ['Gestor'],
  permissions: [PERMISSIONS.DISCIPLINARY_CASES_VIEW],
  ...overrides,
});

export const ok = <T,>(data: T, init?: ResponseInit) => HttpResponse.json({ success: true, data }, init);

export const problem = (status: number, detail: string, extra: Record<string, unknown> = {}) =>
  HttpResponse.json({ status, title: 'Erro', detail, ...extra }, { status });

export const refreshOkResponse = (accessToken: string) => ok({ accessToken, expiresInMinutes: 60 });
export const refreshFailResponse = () => problem(401, 'Sessão ausente ou expirada.');

/** POST /auth/refresh sem cookie válido. */
export const refreshFails = () => http.post(`${API}/auth/refresh`, () => refreshFailResponse());

/** POST /auth/refresh com sucesso, devolvendo o token informado. */
export const refreshSucceeds = (accessToken = 'restored-token', wait = 0) =>
  http.post(`${API}/auth/refresh`, async () => {
    if (wait) await delay(wait);
    return refreshOkResponse(accessToken);
  });

/** GET /auth/me que só aceita o token esperado. */
export const meReturns = (user: CurrentUser, expectedToken?: string) =>
  http.get(`${API}/auth/me`, ({ request }) => {
    if (expectedToken && request.headers.get('Authorization') !== `Bearer ${expectedToken}`) {
      return problem(401, 'Token inválido.');
    }
    return ok(user);
  });

/** Registra cada pathname visitado, para afirmar que uma tela (ex.: /login) nunca apareceu. */
const LocationProbe: React.FC<{ visited: string[] }> = ({ visited }) => {
  const location = useLocation();
  useEffect(() => {
    visited.push(location.pathname);
  }, [location.pathname, visited]);
  return <div data-testid="location">{location.pathname}</div>;
};

/** Renderiza o app real (AuthProvider + rotas) a partir de uma URL. */
export const renderApp = (initialPath = '/', children?: React.ReactNode) => {
  const visited: string[] = [];
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });

  // StrictMode como em main.tsx: os efeitos rodam duas vezes, e a restauração de sessão
  // ainda deve fazer um único refresh.
  const utils = render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <MemoryRouter initialEntries={[initialPath]}>
          <AuthProvider>
            {children ?? <AppRoutes />}
            <LocationProbe visited={visited} />
          </AuthProvider>
        </MemoryRouter>
      </QueryClientProvider>
    </StrictMode>,
  );

  return { ...utils, visited, queryClient };
};
