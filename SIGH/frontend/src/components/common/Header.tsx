import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Typography,
  Box,
  Button,
  IconButton,
  Tooltip,
  Drawer,
  List,
  ListItem,
  ListItemText,
  Switch,
  Divider,
} from '@mui/material';
import { Shield, BookOpen, Gavel, PlusCircle, Settings, CheckCheck, XCircle } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { ALL_DISCIPLINARY_PERMISSIONS } from '../../contexts/AuthContext';
import { NotificationBadge } from '../../features/notifications/components/NotificationBadge';

export const Header: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { permissions, setPermissions, resetAllPermissions, clearAllPermissions } = useAuth();

  const [drawerOpen, setDrawerOpen] = useState(false);

  const handleTogglePermission = (perm: string) => {
    if (permissions.includes(perm)) {
      setPermissions(permissions.filter((p) => p !== perm));
    } else {
      setPermissions([...permissions, perm]);
    }
  };

  return (
    <>
      <AppBar position="static" color="primary" elevation={1}>
        <Toolbar sx={{ justifyContent: 'space-between' }}>
          <Box
            sx={{ display: 'flex', alignItems: 'center', gap: 1.5, cursor: 'pointer' }}
            onClick={() => navigate('/disciplinary/cases')}
          >
            <Shield size={28} color="#d97706" />
            <Typography variant="h6" component="div" sx={{ fontWeight: 700, letterSpacing: '0.02em' }}>
              SIGH
            </Typography>
          </Box>

          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <Button
              color="inherit"
              startIcon={<Gavel size={18} />}
              onClick={() => navigate('/disciplinary/cases')}
              sx={{
                fontWeight: location.pathname.startsWith('/disciplinary/cases') ? 700 : 500,
                borderBottom: location.pathname.startsWith('/disciplinary/cases') ? '2px solid #ffffff' : 'none',
                borderRadius: 0,
              }}
            >
              Processos
            </Button>

            <Button
              color="inherit"
              onClick={() => navigate('/disciplinary/reports/dashboard')}
              sx={{
                fontWeight: location.pathname.startsWith('/disciplinary/reports/dashboard') ? 700 : 500,
                borderBottom: location.pathname.startsWith('/disciplinary/reports/dashboard') ? '2px solid #ffffff' : 'none',
                borderRadius: 0,
              }}
            >
              Dashboard
            </Button>

            <Button
              color="inherit"
              onClick={() => navigate('/disciplinary/reports/cases')}
              sx={{
                fontWeight: location.pathname.startsWith('/disciplinary/reports/cases') ? 700 : 500,
                borderBottom: location.pathname.startsWith('/disciplinary/reports/cases') ? '2px solid #ffffff' : 'none',
                borderRadius: 0,
              }}
            >
              Relatórios
            </Button>

            <Button
              color="inherit"
              onClick={() => navigate('/disciplinary/reports/measures')}
              sx={{
                fontWeight: location.pathname.startsWith('/disciplinary/reports/measures') ? 700 : 500,
                borderBottom: location.pathname.startsWith('/disciplinary/reports/measures') ? '2px solid #ffffff' : 'none',
                borderRadius: 0,
              }}
            >
              Medidas
            </Button>

            <Button
              color="inherit"
              onClick={() => navigate('/disciplinary/reports/employees/history')}
              sx={{
                fontWeight: location.pathname.startsWith('/disciplinary/reports/employees') ? 700 : 500,
                borderBottom: location.pathname.startsWith('/disciplinary/reports/employees') ? '2px solid #ffffff' : 'none',
                borderRadius: 0,
              }}
            >
              Histórico
            </Button>

            <Button
              color="inherit"
              startIcon={<BookOpen size={18} />}
              onClick={() => navigate('/disciplinary/infraction-types')}
              sx={{
                fontWeight: location.pathname.startsWith('/disciplinary/infraction-types') ? 700 : 500,
                borderBottom: location.pathname.startsWith('/disciplinary/infraction-types') ? '2px solid #ffffff' : 'none',
                borderRadius: 0,
              }}
            >
              Tipos de Infração
            </Button>

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

            <NotificationBadge />

            {import.meta.env.DEV && (
              <Tooltip title="Simular Permissões / Auditoria (DEV)">
                <IconButton color="inherit" onClick={() => setDrawerOpen(true)} sx={{ ml: 1 }}>
                  <Settings size={20} />
                </IconButton>
              </Tooltip>
            )}
          </Box>
        </Toolbar>
      </AppBar>

      {/* Drawer de Auditoria/Permissões (Apenas DEV) */}
      {import.meta.env.DEV && (
        <Drawer anchor="right" open={drawerOpen} onClose={() => setDrawerOpen(false)}>
          <Box sx={{ width: 340, p: 2.5 }}>
            <Typography variant="h6" sx={{ fontWeight: 700, mb: 1 }}>
              Simulador de Permissões (Modo Dev)
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
              Ligue ou desligue permissões para auditar o comportamento da interface e bloqueio de ações.
            </Typography>

            <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
              <Button
                size="small"
                variant="outlined"
                color="success"
                startIcon={<CheckCheck size={16} />}
                onClick={resetAllPermissions}
                fullWidth
              >
                Marcar Todas
              </Button>
              <Button
                size="small"
                variant="outlined"
                color="error"
                startIcon={<XCircle size={16} />}
                onClick={clearAllPermissions}
                fullWidth
              >
                Desmarcar Todas
              </Button>
            </Box>

            <Divider sx={{ my: 2 }} />

            <List>
              {ALL_DISCIPLINARY_PERMISSIONS.filter((p, i, self) => self.indexOf(p) === i).map((perm) => {
                const isChecked = permissions.includes(perm);
                return (
                  <ListItem key={perm} sx={{ px: 0, py: 0.5 }}>
                    <ListItemText
                      primary={perm.replace('Disciplinary.', '')}
                      slotProps={{
                        primary: { sx: { fontSize: '0.8rem', fontWeight: 600 } },
                      }}
                    />
                    <Switch
                      edge="end"
                      size="small"
                      checked={isChecked}
                      onChange={() => handleTogglePermission(perm)}
                    />
                  </ListItem>
                );
              })}
            </List>
          </Box>
        </Drawer>
      )}
    </>
  );
};
