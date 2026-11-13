import axios, { AxiosError, type AxiosRequestConfig } from 'axios';
import { tokenStore } from '../auth/tokenStore';
import { crossTab } from '../auth/crossTab';

declare module 'axios' {
  interface AxiosRequestConfig {
    /** Não tenta renovar o token em caso de 401 (usado pelas próprias rotas de autenticação). */
    skipAuthRefresh?: boolean;
    /** Interno: a requisição já foi repetida uma vez após um refresh. */
    _retried?: boolean;
    /** Interno: access token enviado nesta requisição. */
    _sentToken?: string | null;
  }
}

export const PASSWORD_CHANGE_REQUIRED = 'PASSWORD_CHANGE_REQUIRED';

// Mesma origem: em dev o Vite faz proxy de /api para a API .NET (vite.config.ts);
// em outros ambientes o front deve ser servido junto da API ou atrás de um proxy reverso.
export const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10000,
});

export interface AuthEventHandlers {
  /** O refresh falhou: a sessão acabou. */
  onSessionExpired: () => void;
  /** A API recusou a requisição porque a troca de senha é obrigatória. */
  onPasswordChangeRequired: () => void;
  /**
   * A sessão foi renovada porque o acesso mudou (perfis/gerências/stamp): as permissões exibidas
   * precisam ser recarregadas de /auth/me.
   */
  onAccessChanged?: () => void;
}

/** Códigos de 401 do SecurityStampMiddleware: o acesso mudou depois da emissão do token. */
export const ACCESS_CHANGED_CODES = ['ACCESS_CHANGED', 'SESSION_REVOKED'];

// Sem AuthProvider montado (ex.: chamada fora da árvore React), recorre a navegação completa.
const defaultHandlers: AuthEventHandlers = {
  onSessionExpired: () => window.location.assign('/login'),
  onPasswordChangeRequired: () => window.location.assign('/change-password'),
};

let handlers: AuthEventHandlers = defaultHandlers;

/** Registra quem reage a fim de sessão e troca de senha obrigatória. Retorna a função que desfaz o registro. */
export const setAuthEventHandlers = (next: AuthEventHandlers): (() => void) => {
  handlers = next;
  return () => {
    if (handlers === next) handlers = defaultHandlers;
  };
};

interface RefreshResult {
  success: boolean;
  data?: { accessToken: string; expiresInMinutes: number };
}

let refreshInFlight: Promise<string> | null = null;

const requestNewAccessToken = async (): Promise<string> => {
  const response = await api.post<RefreshResult>('/v1/auth/refresh', null, { withCredentials: true, skipAuthRefresh: true });
  const token = response.data.data?.accessToken;
  if (!token) throw new Error('Resposta de refresh sem access token.');
  return token;
};

/**
 * Renova o access token com o cookie HttpOnly sigh_rt.
 * - Single-flight na aba: chamadas simultâneas compartilham a mesma requisição.
 * - Entre abas: o refresh roda com o lock "sigh-refresh"; se, enquanto esperava o lock, outra aba
 *   renovou e publicou o token no BroadcastChannel, ele é reaproveitado sem nova requisição
 *   (o backend rotaciona o refresh token a cada uso).
 */
export const refreshAccessToken = (): Promise<string> => {
  if (!refreshInFlight) {
    const tokenBeforeWaiting = tokenStore.get();
    refreshInFlight = crossTab
      .withRefreshLock(async () => {
        const current = tokenStore.get();
        if (current && current !== tokenBeforeWaiting) return current;

        const token = await requestNewAccessToken();
        tokenStore.set(token);
        crossTab.publish({ type: 'token', accessToken: token });
        return token;
      })
      .catch((error: unknown) => {
        tokenStore.clear();
        throw error;
      })
      .finally(() => {
        refreshInFlight = null;
      });
  }
  return refreshInFlight;
};

// Mensagens das outras abas.
crossTab.subscribe((message) => {
  if (message.type === 'token') {
    // Só aproveita o token se esta aba tem sessão (ou está renovando): uma aba deslogada não
    // deve "entrar" sozinha.
    if (tokenStore.get() !== null || refreshInFlight !== null) tokenStore.set(message.accessToken);
    return;
  }
  if (message.type === 'logout') {
    tokenStore.clear();
    handlers.onSessionExpired();
  }
});

api.interceptors.request.use((config) => {
  const token = tokenStore.get();
  config._sentToken = token;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<{ code?: string }>) => {
    const config = error.config as AxiosRequestConfig | undefined;
    const status = error.response?.status;

    if (status === 403 && error.response?.data?.code === PASSWORD_CHANGE_REQUIRED) {
      handlers.onPasswordChangeRequired();
      return Promise.reject(error);
    }

    if (status !== 401 || !config || config.skipAuthRefresh || config._retried) {
      return Promise.reject(error);
    }

    config._retried = true;

    // Se outro 401 já renovou o token depois que esta requisição saiu, só repete com o token novo.
    const current = tokenStore.get();
    if (current && current !== config._sentToken) {
      return api(config);
    }

    try {
      await refreshAccessToken();
    } catch {
      handlers.onSessionExpired();
      return Promise.reject(error);
    }

    // Renovou porque o acesso mudou: o novo token já traz as permissões recalculadas; a tela também.
    if (ACCESS_CHANGED_CODES.includes(error.response?.data?.code ?? '')) handlers.onAccessChanged?.();

    return api(config);
  },
);

export default api;
