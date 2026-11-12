import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, delay } from 'msw';
import { server } from '../../test/server';
import { API, makeUser, meReturns, ok, refreshSucceeds, renderApp } from '../../test/auth';
import { api, refreshAccessToken } from '../../services/api';
import { AUTH_CHANNEL_NAME, REFRESH_LOCK_NAME, type CrossTabMessage } from '../crossTab';
import { tokenStore } from '../tokenStore';

/** LockManager mínimo (o jsdom não tem Web Locks): fila FIFO por nome, como o navegador. */
class FakeLockManager {
  requested: string[] = [];
  private tails = new Map<string, Promise<unknown>>();

  request(name: string, optionsOrCallback: unknown, maybeCallback?: () => Promise<unknown>) {
    const callback = (typeof optionsOrCallback === 'function' ? optionsOrCallback : maybeCallback) as () => Promise<unknown>;
    this.requested.push(name);
    const previous = this.tails.get(name) ?? Promise.resolve();
    const run = previous.then(() => callback());
    this.tails.set(name, run.catch(() => undefined));
    return run;
  }

  /** Simula outra aba segurando o lock até release() ser chamado. */
  holdByOtherTab(name: string): () => void {
    let release!: () => void;
    void this.request(name, () => new Promise<void>((resolve) => (release = resolve)));
    return () => release();
  }
}

/** A "outra aba": um segundo BroadcastChannel com o mesmo nome. */
const otherTab = () => {
  const channel = new BroadcastChannel(AUTH_CHANNEL_NAME);
  const received: CrossTabMessage[] = [];
  channel.onmessage = (event: MessageEvent<CrossTabMessage>) => received.push(event.data);
  return { channel, received };
};

describe('refresh between tabs (Web Locks + BroadcastChannel)', () => {
  let locks: FakeLockManager;
  const channels: BroadcastChannel[] = [];

  beforeEach(() => {
    locks = new FakeLockManager();
    vi.stubGlobal('navigator', Object.assign(Object.create(navigator), { locks }));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    channels.splice(0).forEach((c) => c.close());
  });

  it('runs the refresh inside the "sigh-refresh" lock and publishes the new token to the other tabs', async () => {
    const tab = otherTab();
    channels.push(tab.channel);
    server.use(refreshSucceeds('token-from-this-tab'));

    await expect(refreshAccessToken()).resolves.toBe('token-from-this-tab');

    expect(locks.requested).toEqual([REFRESH_LOCK_NAME]);
    await waitFor(() => expect(tab.received).toContainEqual({ type: 'token', accessToken: 'token-from-this-tab' }));
  });

  it('reuses the token another tab published while this tab waited for the lock, without calling /auth/refresh', async () => {
    tokenStore.set('expired-token');
    let refreshCalls = 0;
    server.use(
      http.post(`${API}/auth/refresh`, () => {
        refreshCalls += 1;
        return ok({ accessToken: 'should-not-be-used', expiresInMinutes: 60 });
      }),
    );
    const tab = otherTab();
    channels.push(tab.channel);

    // A outra aba está renovando (segura o lock).
    const release = locks.holdByOtherTab(REFRESH_LOCK_NAME);
    const pending = refreshAccessToken();

    // Ela termina, publica o token novo e solta o lock.
    tab.channel.postMessage({ type: 'token', accessToken: 'token-from-other-tab' } satisfies CrossTabMessage);
    await waitFor(() => expect(tokenStore.get()).toBe('token-from-other-tab'));
    release();

    await expect(pending).resolves.toBe('token-from-other-tab');
    expect(refreshCalls).toBe(0);
  });

  it('refreshes normally after waiting for the lock if the other tab published nothing', async () => {
    tokenStore.set('expired-token');
    let refreshCalls = 0;
    server.use(
      http.post(`${API}/auth/refresh`, async () => {
        refreshCalls += 1;
        await delay(5);
        return ok({ accessToken: 'fresh', expiresInMinutes: 60 });
      }),
    );

    const release = locks.holdByOtherTab(REFRESH_LOCK_NAME);
    const pending = refreshAccessToken();
    await delay(20);
    expect(refreshCalls).toBe(0); // ainda esperando o lock
    release();

    await expect(pending).resolves.toBe('fresh');
    expect(refreshCalls).toBe(1);
  });

  it('a 401 retried after another tab refreshed uses the broadcast token', async () => {
    tokenStore.set('old-token');
    server.use(
      http.get(`${API}/items/1`, ({ request }) =>
        request.headers.get('Authorization') === 'Bearer shared-token' ? ok({ id: 1 }) : new Response(null, { status: 401 }),
      ),
      http.post(`${API}/auth/refresh`, () => ok({ accessToken: 'must-not-be-used', expiresInMinutes: 60 })),
    );
    const tab = otherTab();
    channels.push(tab.channel);

    const release = locks.holdByOtherTab(REFRESH_LOCK_NAME);
    const request = api.get('/v1/items/1');
    await delay(20);
    tab.channel.postMessage({ type: 'token', accessToken: 'shared-token' } satisfies CrossTabMessage);
    await waitFor(() => expect(tokenStore.get()).toBe('shared-token'));
    release();

    await expect(request).resolves.toMatchObject({ data: { data: { id: 1 } } });
  });

  it('ignores a token broadcast while this tab has no session', async () => {
    const tab = otherTab();
    channels.push(tab.channel);

    tab.channel.postMessage({ type: 'token', accessToken: 'someone-else' } satisfies CrossTabMessage);
    await delay(20);

    expect(tokenStore.get()).toBeNull();
  });
});

describe('without the Web Locks API', () => {
  it('falls back to the in-tab single-flight', async () => {
    expect((navigator as Navigator & { locks?: unknown }).locks).toBeUndefined();
    tokenStore.set('expired');
    let refreshCalls = 0;
    server.use(
      http.post(`${API}/auth/refresh`, async () => {
        refreshCalls += 1;
        await delay(20);
        return ok({ accessToken: 'fresh', expiresInMinutes: 60 });
      }),
    );

    const tokens = await Promise.all([refreshAccessToken(), refreshAccessToken(), refreshAccessToken()]);

    expect(tokens).toEqual(['fresh', 'fresh', 'fresh']);
    expect(refreshCalls).toBe(1);
  });
});

describe('logout between tabs', () => {
  const channels: BroadcastChannel[] = [];
  afterEach(() => channels.splice(0).forEach((c) => c.close()));

  it('a logout in another tab sends this tab to /login', async () => {
    server.use(refreshSucceeds('t'), meReturns(makeUser({ fullName: 'Maria Gestora' })));
    const tab = otherTab();
    channels.push(tab.channel);

    renderApp('/');
    expect(await screen.findByTestId('current-user-name')).toHaveTextContent('Maria Gestora');

    tab.channel.postMessage({ type: 'logout' } satisfies CrossTabMessage);

    expect(await screen.findByRole('heading', { name: 'Entrar no SIGH' })).toBeInTheDocument();
    expect(tokenStore.get()).toBeNull();
  });

  it('logging out here tells the other tabs', async () => {
    server.use(refreshSucceeds('t'), meReturns(makeUser()), http.post(`${API}/auth/logout`, () => ok({ success: true })));
    const tab = otherTab();
    channels.push(tab.channel);

    renderApp('/');
    await screen.findByTestId('current-user-name');
    await userEvent.setup().click(screen.getByRole('button', { name: 'Sair' }));

    await waitFor(() => expect(tab.received).toContainEqual({ type: 'logout' }));
  });
});
