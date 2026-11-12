import axios, { AxiosError, type AxiosRequestConfig } from 'axios';
import { tokenStore } from '../auth/tokenStore';

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
}

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

/**
 * Renova o access token com o cookie HttpOnly sigh_rt. Single-flight: chamadas simultâneas
 * compartilham a mesma requisição, porque o backend rotaciona o refresh token a cada uso
 * e um segundo refresh com o cookie antigo seria tratado como reutilização.
 */
export const refreshAccessToken = (): Promise<string> => {
  if (!refreshInFlight) {
    refreshInFlight = api
      .post<RefreshResult>('/v1/auth/refresh', null, { withCredentials: true, skipAuthRefresh: true })
      .then((response) => {
        const token = response.data.data?.accessToken;
        if (!token) throw new Error('Resposta de refresh sem access token.');
        tokenStore.set(token);
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

    return api(config);
  },
);

export default api;
