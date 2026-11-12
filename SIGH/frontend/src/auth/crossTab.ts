// Coordenação entre abas do mesmo navegador (mesmo cookie sigh_rt):
// - o refresh é serializado com a Web Locks API ("sigh-refresh"): só uma aba renova por vez;
// - quem renova publica o novo access token no BroadcastChannel "sigh-auth", e as outras abas
//   o reaproveitam em vez de renovar de novo com um cookie que acabou de ser rotacionado;
// - o logout de uma aba avisa as outras, que voltam para /login.
// Sem navigator.locks (navegador antigo, testes), vale só o single-flight dentro da aba.

export const AUTH_CHANNEL_NAME = 'sigh-auth';
export const REFRESH_LOCK_NAME = 'sigh-refresh';

export type CrossTabMessage = { type: 'token'; accessToken: string } | { type: 'logout' };

type Listener = (message: CrossTabMessage) => void;

const listeners = new Set<Listener>();
let channel: BroadcastChannel | null = null;

const getChannel = (): BroadcastChannel | null => {
  if (channel || typeof BroadcastChannel === 'undefined') return channel;
  channel = new BroadcastChannel(AUTH_CHANNEL_NAME);
  channel.onmessage = (event: MessageEvent<CrossTabMessage>) => {
    const message = event.data;
    if (!message || (message.type !== 'token' && message.type !== 'logout')) return;
    listeners.forEach((listener) => listener(message));
  };
  // Node (testes) mantém o processo vivo com um canal aberto; no navegador unref não existe.
  (channel as unknown as { unref?: () => void }).unref?.();
  return channel;
};

export const crossTab = {
  /** Recebe mensagens das OUTRAS abas (o BroadcastChannel não entrega para quem enviou). */
  subscribe(listener: Listener): () => void {
    getChannel();
    listeners.add(listener);
    return () => listeners.delete(listener);
  },

  publish(message: CrossTabMessage): void {
    getChannel()?.postMessage(message);
  },

  /** Executa fn com o lock de refresh entre abas, se a Web Locks API existir. */
  withRefreshLock<T>(fn: () => Promise<T>): Promise<T> {
    const locks = typeof navigator !== 'undefined' ? navigator.locks : undefined;
    if (!locks?.request) return fn();
    return locks.request(REFRESH_LOCK_NAME, { mode: 'exclusive' }, fn) as Promise<T>;
  },

  /** Só para testes: fecha o canal para que o próximo uso crie um novo. */
  resetForTests(): void {
    channel?.close();
    channel = null;
    listeners.clear();
  },
};
