import React from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { AppBar, Toolbar, Typography, Box, Button, IconButton, Tooltip } from '@mui/material';
import { Shield, BookOpen, Gavel, PlusCircle, LogOut, UserRound, ScrollText, Users } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { PERMISSIONS, type PermissionCode } from '../../auth/permissions';
import { NotificationBadge } from '../../features/notifications/components/NotificationBadge';

interface MenuItem {
  label: string;
  path: string;
  /** Prefixo usado para destacar o item ativo. */
  activePrefix: string;
  permission: PermissionCode;
  icon?: React.ReactNode;
}

export const MENU_ITEMS: MenuItem[] = [
  {
    label: 'Processos',
    path: '/disciplinary/cases',
    activePrefix: '/disciplinary/cases',
    permission: PERMISSIONS.DISCIPLINARY_CASES_VIEW,
    icon: <Gavel size={18} />,
  },
  {
    label: 'Dashboard',
    path: '/disciplinary/reports/dashboard',
    activePrefix: '/disciplinary/reports/dashboard',
    permission: PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_DASHBOARD,
  },
  {
    label: 'Relatórios',
    path: '/disciplinary/reports/cases',
    activePrefix: '/disciplinary/reports/cases',
    permission: PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_CASES,
  },
  {
    label: 'Medidas',
    path: '/disciplinary/reports/measures',
    activePrefix: '/disciplinary/reports/measures',
    permission: PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES,
  },
  {
    label: 'Histórico',
    path: '/disciplinary/reports/employees/history',
    activePrefix: '/disciplinary/reports/employees',
    permission: PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY,
  },
  {
    label: 'Tipos de Infração',
    path: '/disciplinary/infraction-types',
    activePrefix: '/disciplinary/infraction-types',
    permission: PERMISSIONS.DISCIPLINARY_INFRACTION_TYPES_VIEW,
    icon: <BookOpen size={18} />,
  },
  {
    label: 'Funcionários',
    path: '/employees',
    activePrefix: '/employees',
    permission: PERMISSIONS.EMPLOYEES_VIEW,
    icon: <Users size={18} />,
  },
  {
    label: 'Auditoria',
    path: '/audit',
    activePrefix: '/audit',
    permission: PERMISSIONS.AUDIT_VIEW,
    icon: <ScrollText size={18} />,
  },
];

export const Header: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, hasPermission, logout } = useAuth();

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <AppBar position="static" color="primary" elevation={1}>
      <Toolbar sx={{ justifyContent: 'space-between', gap: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, cursor: 'pointer' }} onClick={() => navigate('/')}>
          <Shield size={28} color="#d97706" />
          <Typography variant="h6" component="div" sx={{ fontWeight: 700, letterSpacing: '0.02em' }}>
            SIGH
          </Typography>
        </Box>

        <Box component="nav" aria-label="Menu principal" sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
          {MENU_ITEMS.filter((item) => hasPermission(item.permission)).map((item) => {
            const active = location.pathname.startsWith(item.activePrefix);
            return (
              <Button
                key={item.path}
                color="inherit"
                startIcon={item.icon}
                onClick={() => navigate(item.path)}
                aria-current={active ? 'page' : undefined}
                sx={{
                  fontWeight: active ? 700 : 500,
                  borderBottom: active ? '2px solid #ffffff' : 'none',
                  borderRadius: 0,
                }}
              >
                {item.label}
              </Button>
            );
          })}

          {hasPermission(PERMISSIONS.DISCIPLINARY_CASES_CREATE) && (
            <Button
              variant="contained"
              color="secondary"
              size="small"
              startIcon={<PlusCircle size={16} />}
              onClick={() => navigate('/disciplinary/cases/new')}
              sx={{ ml: 1, fontWeight: 700 }}
            >
              Novo Processo
            </Button>
          )}

          {hasPermission(PERMISSIONS.NOTIFICATIONS_VIEW) && <NotificationBadge />}

          {user && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75, ml: 1 }}>
              <UserRound size={18} />
              <Typography variant="body2" sx={{ fontWeight: 600 }} data-testid="current-user-name">
                {user.fullName}
              </Typography>
            </Box>
          )}

          <Tooltip title="Sair">
            <IconButton color="inherit" onClick={handleLogout} aria-label="Sair">
              <LogOut size={20} />
            </IconButton>
          </Tooltip>
        </Box>
      </Toolbar>
    </AppBar>
  );
};
