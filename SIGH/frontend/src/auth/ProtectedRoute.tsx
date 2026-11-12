import React from 'react';
import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import { FullScreenLoader } from '../components/common/FullScreenLoader';

export const CHANGE_PASSWORD_PATH = '/change-password';

/**
 * Exige sessão. Enquanto a sessão é restaurada mostra um carregamento (sem piscar o login).
 * Com mustChangePassword, só /change-password é acessível.
 */
export const ProtectedRoute: React.FC<{ children?: React.ReactNode }> = ({ children }) => {
  const { status, user } = useAuth();
  const location = useLocation();

  if (status === 'loading') {
    return <FullScreenLoader label="Carregando sessão..." />;
  }

  if (status === 'unauthenticated' || !user) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  if (user.mustChangePassword && location.pathname !== CHANGE_PASSWORD_PATH) {
    return <Navigate to={CHANGE_PASSWORD_PATH} replace />;
  }

  return children ? <>{children}</> : <Outlet />;
};
