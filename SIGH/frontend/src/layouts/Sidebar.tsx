import React, { useState } from 'react';
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ListItemIcon, Menu, MenuItem as MuiMenuItem, Tooltip } from '@mui/material';
import { ChevronsUpDown, KeyRound, LogOut, Moon, Sun } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { useThemeContext } from '../contexts/ThemeContext';
import { PERMISSIONS } from '../auth/permissions';
import { SighLogo } from '../components/brand/SighLogo';
import { InitialsAvatar } from '../components/ui/InitialsAvatar';
import { recordKeys, recordsApi } from '../features/records/api/recordsApi';
import { label } from '../i18n/labels';
import { formatNumber } from '../utils/formatters';
import { ADMIN_MENU, MAIN_MENU, isItemActive, type MenuItem } from './menu';

const PendingBadge: React.FC<{ collapsed: boolean }> = ({ collapsed }) => {
  const { data } = useQuery({ queryKey: recordKeys.pendingCount, queryFn: recordsApi.pendingCount, refetchInterval: 60_000 });
  const count = data ?? 0;
  return (
    <span
      data-testid="pending-approvals-count"
      aria-label={`${count} aguardando aprovação`}
      className={`inline-flex min-w-5 items-center justify-center rounded-full px-1.5 text-[0.7rem] font-bold ${
        count > 0 ? 'bg-danger-solid text-white' : 'bg-surface-3 text-muted'
      } ${collapsed ? 'absolute right-1.5 top-1' : 'ml-auto'}`}
    >
      {formatNumber(count)}
    </span>
  );
};

const NavItem: React.FC<{ item: MenuItem; collapsed: boolean; onNavigate?: () => void }> = ({ item, collapsed, onNavigate }) => {
  const { pathname } = useLocation();
  const active = isItemActive(item, pathname);
  const link = (
    <RouterLink
      to={item.path}
      onClick={onNavigate}
      aria-current={active ? 'page' : undefined}
      data-testid={item.testId}
      className={`relative flex items-center gap-3 rounded-md px-3 py-2.5 text-sm font-medium transition-colors ${
        active ? 'bg-primary text-white shadow-card' : 'text-muted hover:bg-surface-2 hover:text-ink'
      } ${collapsed ? 'justify-center' : ''}`}
    >
      <span aria-hidden="true" className="shrink-0">
        {item.icon}
      </span>
      <span className={collapsed ? 'sr-only' : ''}>{item.label}</span>
      {item.badge === 'pending-approvals' && <PendingBadge collapsed={collapsed} />}
    </RouterLink>
  );
  return <li>{collapsed ? <Tooltip title={item.label} placement="right">{link}</Tooltip> : link}</li>;
};

/** Menu lateral: logo, itens do perfil, grupo Administração e, no rodapé, o usuário com o menu e "Sair". */
export const Sidebar: React.FC<{ collapsed: boolean; onNavigate?: () => void }> = ({ collapsed, onNavigate }) => {
  const navigate = useNavigate();
  const { user, hasPermission, logout } = useAuth();
  const { mode, toggleTheme } = useThemeContext();
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const main = MAIN_MENU.filter((i) => hasPermission(i.permission));
  const admin = ADMIN_MENU.filter((i) => hasPermission(i.permission));
  const roleLabel = user?.roles.length ? user.roles.map((r) => label('Role', r)).join(', ') : '—';

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="flex h-full flex-col bg-sidebar">
      <div className={`flex h-[var(--sigh-topbar-height)] items-center ${collapsed ? 'justify-center' : 'px-5'}`}>
        <RouterLink to="/" aria-label="SIGH — página inicial" className="text-ink" onClick={onNavigate}>
          <SighLogo variant={collapsed ? 'mark' : 'full'} size={32} />
        </RouterLink>
      </div>

      <nav aria-label="Menu principal" className="flex-1 overflow-y-auto px-3 py-3">
        <ul className="flex flex-col gap-1">
          {main.map((item) => (
            <NavItem key={item.path} item={item} collapsed={collapsed} onNavigate={onNavigate} />
          ))}
        </ul>
        {admin.length > 0 && (
          <>
            <p className={`mb-1 mt-5 px-3 text-xs font-semibold uppercase tracking-wider text-muted ${collapsed ? 'sr-only' : ''}`}>Administração</p>
            <ul className="flex flex-col gap-1" aria-label="Administração">
              {admin.map((item) => (
                <NavItem key={item.path} item={item} collapsed={collapsed} onNavigate={onNavigate} />
              ))}
            </ul>
          </>
        )}
      </nav>

      <div className="border-t border-line px-3 py-3">
        <button
          type="button"
          onClick={(e) => setAnchor(e.currentTarget)}
          aria-haspopup="menu"
          aria-expanded={Boolean(anchor)}
          aria-label="Menu do usuário"
          className={`flex w-full items-center gap-3 rounded-md px-2 py-2 text-left hover:bg-surface-2 ${collapsed ? 'justify-center' : ''}`}
        >
          <InitialsAvatar name={user?.fullName} size={36} />
          {!collapsed && (
            <>
              <span className="min-w-0 flex-1">
                <span className="block truncate text-sm font-semibold text-ink" data-testid="current-user-name">
                  {user?.fullName}
                </span>
                <span className="block truncate text-xs text-muted" data-testid="current-user-role">
                  {roleLabel}
                </span>
              </span>
              <ChevronsUpDown size={16} className="text-muted" aria-hidden="true" />
            </>
          )}
        </button>
        <Menu anchorEl={anchor} open={Boolean(anchor)} onClose={() => setAnchor(null)} anchorOrigin={{ vertical: 'top', horizontal: 'right' }} transformOrigin={{ vertical: 'bottom', horizontal: 'left' }}>
          <MuiMenuItem
            onClick={() => {
              setAnchor(null);
              onNavigate?.();
              navigate('/change-password');
            }}
          >
            <ListItemIcon>
              <KeyRound size={17} />
            </ListItemIcon>
            Trocar senha
          </MuiMenuItem>
          <MuiMenuItem
            onClick={() => {
              setAnchor(null);
              toggleTheme();
            }}
          >
            <ListItemIcon>{mode === 'dark' ? <Sun size={17} /> : <Moon size={17} />}</ListItemIcon>
            {mode === 'dark' ? 'Tema claro' : 'Tema escuro'}
          </MuiMenuItem>
        </Menu>
        <button
          type="button"
          onClick={handleLogout}
          aria-label="Sair"
          className={`mt-1 flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-muted hover:bg-surface-2 hover:text-ink ${collapsed ? 'justify-center' : ''}`}
        >
          <LogOut size={18} aria-hidden="true" />
          <span className={collapsed ? 'sr-only' : ''}>Sair</span>
        </button>
      </div>
    </div>
  );
};

export const canSeeApprovals = (has: (p: string) => boolean) => has(PERMISSIONS.DISCIPLINARY_RECORDS_APPROVE);
