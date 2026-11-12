import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { authApi, type CurrentUser } from '../auth/authApi';
import { tokenStore } from '../auth/tokenStore';
import { crossTab } from '../auth/crossTab';
import { refreshAccessToken, setAuthEventHandlers } from '../services/api';

export type AuthStatus = 'loading' | 'authenticated' | 'unauthenticated';

interface AuthContextType {
  status: AuthStatus;
  user: CurrentUser | null;
  isAuthenticated: boolean;
  permissions: string[];
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  /** Autentica e carrega o usuário de GET /auth/me. */
  login: (email: string, password: string) => Promise<CurrentUser>;
  /** Troca a senha, passa a usar o novo access token e recarrega o usuário. */
  changePassword: (currentPassword: string, newPassword: string) => Promise<CurrentUser>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const queryClient = useQueryClient();
  const [status, setStatus] = useState<AuthStatus>('loading');
  const [user, setUser] = useState<CurrentUser | null>(null);

  const endSession = useCallback(() => {
    tokenStore.clear();
    queryClient.clear();
    setUser(null);
    setStatus('unauthenticated');
  }, [queryClient]);

  const loadUser = useCallback(async () => {
    const me = await authApi.me();
    setUser(me);
    setStatus('authenticated');
    return me;
  }, []);

  // Restauração de sessão: o access token não sobrevive ao reload; o cookie HttpOnly sim.
  useEffect(() => {
    let cancelled = false;

    refreshAccessToken()
      .then(() => (cancelled ? null : authApi.me()))
      .then((me) => {
        if (cancelled || !me) return;
        setUser(me);
        setStatus('authenticated');
      })
      .catch(() => {
        if (cancelled) return;
        tokenStore.clear();
        setUser(null);
        setStatus('unauthenticated');
      });

    return () => {
      cancelled = true;
    };
  }, []);

  // O interceptor do axios avisa quando o refresh falha ou a API exige troca de senha.
  // As rotas reagem ao estado: ProtectedRoute leva para /login ou /change-password.
  useEffect(
    () =>
      setAuthEventHandlers({
        onSessionExpired: endSession,
        onPasswordChangeRequired: () =>
          setUser((current) => (current ? { ...current, mustChangePassword: true } : current)),
      }),
    [endSession],
  );

  const login = useCallback(
    async (email: string, password: string) => {
      const result = await authApi.login(email, password);
      tokenStore.set(result.accessToken);
      try {
        return await loadUser();
      } catch (error) {
        endSession();
        throw error;
      }
    },
    [endSession, loadUser],
  );

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      const result = await authApi.changePassword(currentPassword, newPassword);
      tokenStore.set(result.accessToken);
      return loadUser();
    },
    [loadUser],
  );

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } catch {
      // Mesmo que a API falhe (token expirado, rede), a sessão local termina.
    } finally {
      endSession();
      // As outras abas usam o mesmo cookie, que acabou de ser revogado: elas também saem.
      crossTab.publish({ type: 'logout' });
    }
  }, [endSession]);

  const value = useMemo<AuthContextType>(() => {
    const permissions = user?.permissions ?? [];
    const hasPermission = (permission: string) => permissions.includes(permission);
    return {
      status,
      user,
      isAuthenticated: status === 'authenticated' && user !== null,
      permissions,
      hasPermission,
      hasAnyPermission: (perms: string[]) => perms.some(hasPermission),
      login,
      changePassword,
      logout,
    };
  }, [status, user, login, changePassword, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuthContext = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuthContext deve ser usado dentro de um AuthProvider');
  }
  return context;
};
