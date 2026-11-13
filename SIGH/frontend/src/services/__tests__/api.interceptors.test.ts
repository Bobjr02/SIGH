import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { http, HttpResponse, delay } from 'msw';
import { server } from '../../test/server';
import { API, ok, problem } from '../../test/auth';
import { api, PASSWORD_CHANGE_REQUIRED, setAuthEventHandlers } from '../api';
import { tokenStore } from '../../auth/tokenStore';

describe('api interceptors', () => {
  const onSessionExpired = vi.fn();
  const onPasswordChangeRequired = vi.fn();
  let unregister: () => void;

  beforeEach(() => {
    unregister = setAuthEventHandlers({ onSessionExpired, onPasswordChangeRequired });
  });

  afterEach(() => {
    unregister();
    onSessionExpired.mockReset();
    onPasswordChangeRequired.mockReset();
  });

  it('sends the in-memory access token as Bearer and calls the API under /api', async () => {
    tokenStore.set('token-123');
    let seen: { auth: string | null; path: string } | undefined;
    server.use(
      http.get(`${API}/ping`, ({ request }) => {
        seen = { auth: request.headers.get('Authorization'), path: new URL(request.url).pathname };
        return ok('pong');
      }),
    );

    await api.get('/v1/ping');

    expect(seen).toEqual({ auth: 'Bearer token-123', path: '/api/v1/ping' });
  });

  it('runs a single refresh for 3 simultaneous requests that get 401 and retries each once with the new token', async () => {
    tokenStore.set('expired-token');
    let refreshCalls = 0;
    const tokensSeen: string[] = [];

    server.use(
      http.post(`${API}/auth/refresh`, async () => {
        refreshCalls += 1;
        await delay(50); // mantém o refresh em andamento enquanto os outros 401 chegam
        return ok({ accessToken: 'fresh-token', expiresInMinutes: 60 });
      }),
      http.get(`${API}/items/:id`, ({ request, params }) => {
        const auth = request.headers.get('Authorization') ?? '';
        tokensSeen.push(auth);
        if (auth !== 'Bearer fresh-token') return problem(401, 'Token expirado.');
        return ok({ id: params.id });
      }),
    );

    const results = await Promise.all([api.get('/v1/items/1'), api.get('/v1/items/2'), api.get('/v1/items/3')]);

    expect(refreshCalls).toBe(1);
    expect(results.map((r) => r.data.data.id)).toEqual(['1', '2', '3']);
    expect(tokensSeen.filter((t) => t === 'Bearer expired-token')).toHaveLength(3);
    expect(tokensSeen.filter((t) => t === 'Bearer fresh-token')).toHaveLength(3);
    expect(tokenStore.get()).toBe('fresh-token');
    expect(onSessionExpired).not.toHaveBeenCalled();
  });

  it('when the refresh fails, rejects all pending requests, clears the token and ends the session', async () => {
    tokenStore.set('expired-token');
    let refreshCalls = 0;
    server.use(
      http.post(`${API}/auth/refresh`, async () => {
        refreshCalls += 1;
        await delay(20);
        return problem(401, 'Sessão ausente ou expirada.');
      }),
      http.get(`${API}/items/:id`, () => problem(401, 'Token expirado.')),
    );

    const results = await Promise.allSettled([api.get('/v1/items/1'), api.get('/v1/items/2'), api.get('/v1/items/3')]);

    expect(results.every((r) => r.status === 'rejected')).toBe(true);
    expect(refreshCalls).toBe(1);
    expect(tokenStore.get()).toBeNull();
    expect(onSessionExpired).toHaveBeenCalled();
  });

  it('retries a request only once: a second 401 after the refresh is returned to the caller', async () => {
    tokenStore.set('expired-token');
    let refreshCalls = 0;
    let itemCalls = 0;
    server.use(
      http.post(`${API}/auth/refresh`, () => {
        refreshCalls += 1;
        return ok({ accessToken: 'fresh-token', expiresInMinutes: 60 });
      }),
      http.get(`${API}/items/1`, () => {
        itemCalls += 1;
        return problem(401, 'Ainda não autorizado.');
      }),
    );

    await expect(api.get('/v1/items/1')).rejects.toMatchObject({ response: { status: 401 } });
    expect(refreshCalls).toBe(1);
    expect(itemCalls).toBe(2);
  });

  it('on 403 PASSWORD_CHANGE_REQUIRED notifies the app and does not try to refresh', async () => {
    tokenStore.set('token-with-pwd-change');
    const refresh = vi.fn();
    server.use(
      http.post(`${API}/auth/refresh`, () => {
        refresh();
        return ok({ accessToken: 'x', expiresInMinutes: 60 });
      }),
      http.get(`${API}/items/1`, () => problem(403, 'Troca de senha obrigatória.', { code: PASSWORD_CHANGE_REQUIRED })),
    );

    await expect(api.get('/v1/items/1')).rejects.toMatchObject({ response: { status: 403 } });
    expect(onPasswordChangeRequired).toHaveBeenCalledTimes(1);
    expect(refresh).not.toHaveBeenCalled();
  });

  it('does not treat a plain 403 (missing permission) as password change', async () => {
    server.use(http.get(`${API}/items/1`, () => problem(403, 'Permissões insuficientes.')));

    await expect(api.get('/v1/items/1')).rejects.toMatchObject({ response: { status: 403 } });
    expect(onPasswordChangeRequired).not.toHaveBeenCalled();
  });

  it('does not retry a 401 from a request that opted out (auth routes)', async () => {
    const refresh = vi.fn();
    server.use(
      http.post(`${API}/auth/refresh`, () => {
        refresh();
        return ok({ accessToken: 'x', expiresInMinutes: 60 });
      }),
      http.post(`${API}/auth/login`, () => HttpResponse.json({}, { status: 401 })),
    );

    await expect(api.post('/v1/auth/login', {}, { skipAuthRefresh: true })).rejects.toMatchObject({
      response: { status: 401 },
    });
    expect(refresh).not.toHaveBeenCalled();
  });

  it('after a 401 ACCESS_CHANGED (role expired) it renews and asks the app to reload the permissions', async () => {
    const onAccessChanged = vi.fn();
    unregister();
    unregister = setAuthEventHandlers({ onSessionExpired, onPasswordChangeRequired, onAccessChanged });
    tokenStore.set('old-token');
    server.use(
      http.post(`${API}/auth/refresh`, () => ok({ accessToken: 'recalculated-token', expiresInMinutes: 60 })),
      http.get(`${API}/things`, ({ request }) =>
        request.headers.get('Authorization') === 'Bearer recalculated-token'
          ? ok('ok')
          : HttpResponse.json({ status: 401, code: 'ACCESS_CHANGED' }, { status: 401 }),
      ),
    );

    await api.get('/v1/things');

    expect(onAccessChanged).toHaveBeenCalledTimes(1);
    expect(onSessionExpired).not.toHaveBeenCalled();
  });

  it('a revoked session whose refresh is refused (deactivated user) ends the session', async () => {
    const onAccessChanged = vi.fn();
    unregister();
    unregister = setAuthEventHandlers({ onSessionExpired, onPasswordChangeRequired, onAccessChanged });
    tokenStore.set('old-token');
    server.use(
      http.post(`${API}/auth/refresh`, () => problem(400, 'Usuário inativo, bloqueado ou não encontrado.')),
      http.get(`${API}/things`, () => HttpResponse.json({ status: 401, code: 'SESSION_REVOKED' }, { status: 401 })),
    );

    await expect(api.get('/v1/things')).rejects.toBeTruthy();
    expect(onSessionExpired).toHaveBeenCalledTimes(1);
    expect(onAccessChanged).not.toHaveBeenCalled();
  });
});
