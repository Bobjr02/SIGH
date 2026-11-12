import React from 'react';
import { useAuth } from '../hooks/useAuth';
import { ForbiddenPage } from '../pages/ForbiddenPage';
import type { PermissionCode } from './permissions';

interface RequirePermissionProps {
  /** Exige esta permissão. */
  permission?: PermissionCode;
  /** Exige ao menos uma destas permissões. */
  anyOf?: PermissionCode[];
  children: React.ReactNode;
}

/** Sem a permissão, mostra a página 403 no lugar do conteúdo. */
export const RequirePermission: React.FC<RequirePermissionProps> = ({ permission, anyOf, children }) => {
  const { hasPermission, hasAnyPermission } = useAuth();

  const allowed = (permission ? hasPermission(permission) : true) && (anyOf ? hasAnyPermission(anyOf) : true);

  return allowed ? <>{children}</> : <ForbiddenPage />;
};
