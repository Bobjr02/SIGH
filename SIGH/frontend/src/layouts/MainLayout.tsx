import React, { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { Drawer, IconButton, Tooltip, useMediaQuery } from '@mui/material';
import { Menu as MenuIcon, Moon, Sun } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { useThemeContext } from '../contexts/ThemeContext';
import { PERMISSIONS } from '../auth/permissions';
import { NotificationBadge } from '../features/notifications/components/NotificationBadge';
import { GlobalSearch } from './GlobalSearch';
import { Sidebar } from './Sidebar';

const COLLAPSE_KEY = 'sigh.sidebar.collapsed';

const readCollapsed = () => {
  try {
    return window.localStorage.getItem(COLLAPSE_KEY) === '1';
  } catch {
    return false;
  }
};

/** Botão de tema (lua/sol) da barra superior. */
export const ThemeToggle: React.FC = () => {
  const { mode, toggleTheme } = useThemeContext();
  const text = mode === 'dark' ? 'Usar tema claro' : 'Usar tema escuro';
  return (
    <Tooltip title={text}>
      <IconButton onClick={toggleTheme} aria-label={text} data-testid="theme-toggle">
        {mode === 'dark' ? <Moon size={20} /> : <Sun size={20} />}
      </IconButton>
    </Tooltip>
  );
};

/**
 * Estrutura principal: menu lateral (recolhível pelo ☰; vira gaveta no celular) e barra superior com
 * a busca global de funcionários, o sino de notificações e o botão de tema.
 */
export const MainLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const mobile = useMediaQuery('(max-width: 899.95px)');
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const [collapsed, setCollapsed] = useState(readCollapsed);
  const [drawerOpen, setDrawerOpen] = useState(false);

  useEffect(() => setDrawerOpen(false), [pathname]);

  const toggleMenu = () => {
    if (mobile) {
      setDrawerOpen((o) => !o);
      return;
    }
    setCollapsed((c) => {
      try {
        window.localStorage.setItem(COLLAPSE_KEY, c ? '0' : '1');
      } catch {
        // sem armazenamento: vale só nesta aba
      }
      return !c;
    });
  };

  const width = collapsed ? 'var(--sigh-sidebar-collapsed)' : 'var(--sigh-sidebar-width)';

  return (
    <div className="flex min-h-screen bg-page text-ink">
      {mobile ? (
        <Drawer
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          slotProps={{ paper: { sx: { width: 'var(--sigh-sidebar-width)', backgroundColor: 'var(--sigh-sidebar)', border: 'none' } } }}
        >
          <Sidebar collapsed={false} onNavigate={() => setDrawerOpen(false)} />
        </Drawer>
      ) : (
        <aside
          className="sticky shrink-0 border-r border-line transition-[width] duration-200"
          style={{ width, top: 'var(--sigh-banner-height, 0px)', height: 'calc(100vh - var(--sigh-banner-height, 0px))' }}
          data-testid="sidebar"
        >
          <Sidebar collapsed={collapsed} />
        </aside>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <header style={{ top: 'var(--sigh-banner-height, 0px)' }} className="sticky z-30 flex h-[var(--sigh-topbar-height)] items-center gap-3 border-b border-line bg-sidebar px-3 sm:px-6">
          <Tooltip title={mobile ? 'Abrir menu' : collapsed ? 'Expandir menu' : 'Recolher menu'}>
            <IconButton onClick={toggleMenu} aria-label={mobile ? 'Abrir menu' : collapsed ? 'Expandir menu' : 'Recolher menu'} data-testid="menu-toggle">
              <MenuIcon size={22} />
            </IconButton>
          </Tooltip>
          <div className="flex min-w-0 flex-1 justify-center md:justify-start">{hasPermission(PERMISSIONS.EMPLOYEES_VIEW) && <GlobalSearch />}</div>
          <div className="flex items-center gap-1">
            {hasPermission(PERMISSIONS.NOTIFICATIONS_VIEW) && <NotificationBadge />}
            <ThemeToggle />
          </div>
        </header>
        <main id="conteudo" className="mx-auto w-full max-w-[1400px] flex-1 px-4 py-6 sm:px-8">
          {children}
        </main>
      </div>
    </div>
  );
};
