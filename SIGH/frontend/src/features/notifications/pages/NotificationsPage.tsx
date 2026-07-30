import React, { useState } from 'react';
import {
  Box,
  Typography,
  Button,
  Pagination,
  CircularProgress,
  Alert,
  Paper,
  Stack,
  Snackbar,
} from '@mui/material';
import { Bell, CheckCheck, Play, RefreshCw } from 'lucide-react';
import {
  useGetNotifications,
  useMarkNotificationAsRead,
  useMarkAllNotificationsAsRead,
  useProcessDeadlines,
  useUnreadNotificationsCount,
} from '../hooks/useNotifications';
import { NotificationFiltersPanel } from '../components/NotificationFiltersPanel';
import { NotificationItem } from '../components/NotificationItem';
import { NotificationFilters } from '../types';
import { useAuthContext } from '../../../contexts/AuthContext';

const defaultFilters: NotificationFilters = {
  pageNumber: 1,
  pageSize: 10,
  isRead: null,
  type: null,
  priority: null,
  sortBy: 'CreatedAt',
  sortDirection: 'desc',
};

export const NotificationsPage: React.FC = () => {
  const { hasPermission } = useAuthContext();
  const canManage = hasPermission('Notifications.Manage');

  const [filters, setFilters] = useState<NotificationFilters>(defaultFilters);
  const [snackbarMessage, setSnackbarMessage] = useState<string | null>(null);

  const { data: pagedData, isLoading, isError, error, refetch } = useGetNotifications(filters);
  const { data: unreadCount = 0 } = useUnreadNotificationsCount();

  const markAsReadMutation = useMarkNotificationAsRead();
  const markAllAsReadMutation = useMarkAllNotificationsAsRead();
  const processDeadlinesMutation = useProcessDeadlines();

  const handleFilterChange = (newFilters: Partial<NotificationFilters>) => {
    setFilters((prev) => ({ ...prev, ...newFilters }));
  };

  const handleResetFilters = () => {
    setFilters(defaultFilters);
  };

  const handleMarkAsRead = async (id: string) => {
    try {
      await markAsReadMutation.mutateAsync({ id });
      setSnackbarMessage('Notificação marcada como lida.');
    } catch (err: any) {
      setSnackbarMessage(err.message || 'Erro ao marcar notificação como lida.');
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      const count = await markAllAsReadMutation.mutateAsync();
      setSnackbarMessage(`${count} notificações marcadas como lidas.`);
    } catch (err: any) {
      setSnackbarMessage(err.message || 'Erro ao marcar notificações como lidas.');
    }
  };

  const handleProcessDeadlines = async () => {
    try {
      const res = await processDeadlinesMutation.mutateAsync();
      setSnackbarMessage(`Prazos processados: ${res.remindersSentCount} lembretes e ${res.escalationsProcessedCount} escalonamentos.`);
    } catch (err: any) {
      setSnackbarMessage(err.message || 'Erro ao processar prazos.');
    }
  };

  return (
    <Box>
      {/* Header da Página */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3, flexWrap: 'wrap', gap: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
          <Bell size={28} color="#2563eb" />
          <Typography variant="h4" component="h1" sx={{ fontWeight: 700 }}>
            Notificações
          </Typography>
          {unreadCount > 0 && (
            <Alert severity="warning" sx={{ py: 0, px: 1.5, borderRadius: 4, fontWeight: 700 }}>
              {unreadCount} não lida{unreadCount > 1 ? 's' : ''}
            </Alert>
          )}
        </Box>

        <Stack direction="row" spacing={1.5}>
          <Button
            variant="outlined"
            startIcon={<RefreshCw size={16} />}
            onClick={() => refetch()}
            disabled={isLoading}
          >
            Atualizar
          </Button>

          <Button
            variant="contained"
            color="primary"
            startIcon={<CheckCheck size={18} />}
            onClick={handleMarkAllAsRead}
            disabled={markAllAsReadMutation.isPending || unreadCount === 0}
          >
            Marcar todas como lidas
          </Button>

          {canManage && (
            <Button
              variant="outlined"
              color="secondary"
              startIcon={<Play size={16} />}
              onClick={handleProcessDeadlines}
              disabled={processDeadlinesMutation.isPending}
            >
              Processar Prazos (Manual)
            </Button>
          )}
        </Stack>
      </Box>

      {/* Painel de Filtros */}
      <NotificationFiltersPanel
        filters={filters}
        onChange={handleFilterChange}
        onReset={handleResetFilters}
      />

      {/* Conteúdo da Lista */}
      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
          <CircularProgress />
        </Box>
      ) : isError ? (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error?.message || 'Ocorreu um erro ao carregar as notificações.'}
        </Alert>
      ) : !pagedData || pagedData.items.length === 0 ? (
        <Paper sx={{ p: 5, textAlign: 'center', borderRadius: 2 }}>
          <Typography variant="h6" color="text.secondary" sx={{ mb: 1 }}>
            Nenhuma notificação encontrada
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Não há notificações correspondentes aos critérios de busca selecionados.
          </Typography>
        </Paper>
      ) : (
        <>
          <Box>
            {pagedData.items.map((notification) => (
              <NotificationItem
                key={notification.id}
                notification={notification}
                onMarkAsRead={handleMarkAsRead}
                isMarkingAsRead={markAsReadMutation.isPending}
              />
            ))}
          </Box>

          {/* Paginação */}
          {pagedData.totalPages > 1 && (
            <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3, mb: 2 }}>
              <Pagination
                count={pagedData.totalPages}
                page={filters.pageNumber || 1}
                onChange={(_, page) => handleFilterChange({ pageNumber: page })}
                color="primary"
                shape="rounded"
              />
            </Box>
          )}
        </>
      )}

      {/* Feedback Snackbar */}
      <Snackbar
        open={Boolean(snackbarMessage)}
        autoHideDuration={4000}
        onClose={() => setSnackbarMessage(null)}
        message={snackbarMessage}
      />
    </Box>
  );
};
