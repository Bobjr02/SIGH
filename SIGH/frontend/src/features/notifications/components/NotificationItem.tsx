import React from 'react';
import { Paper, Box, Typography, Chip, Button } from '@mui/material';
import { CheckCircle2, AlertTriangle, Info, Bell, Clock } from 'lucide-react';
import { NotificationDto } from '../types';

interface NotificationItemProps {
  notification: NotificationDto;
  onMarkAsRead: (id: string) => void;
  isMarkingAsRead?: boolean;
}

export const NotificationItem: React.FC<NotificationItemProps> = ({
  notification,
  onMarkAsRead,
  isMarkingAsRead = false,
}) => {
  const getIcon = () => {
    switch (notification.type.toLowerCase()) {
      case 'escalation':
      case 'error':
        return <AlertTriangle size={20} color="#dc2626" />;
      case 'reminder':
      case 'warning':
        return <Clock size={20} color="#d97706" />;
      default:
        return <Info size={20} color="#2563eb" />;
    }
  };

  const getPriorityColor = () => {
    switch (notification.priority.toLowerCase()) {
      case 'critical':
      case 'high':
        return 'error';
      case 'medium':
        return 'warning';
      default:
        return 'default';
    }
  };

  return (
    <Paper
      elevation={notification.isRead ? 0 : 2}
      sx={{
        p: 2.5,
        mb: 2,
        borderRadius: 2,
        borderLeft: 4,
        borderLeftColor: notification.isRead
          ? 'grey.300'
          : notification.priority.toLowerCase() === 'high' || notification.priority.toLowerCase() === 'critical'
          ? 'error.main'
          : 'primary.main',
        backgroundColor: notification.isRead ? 'background.paper' : 'action.hover',
        transition: 'all 0.2s ease-in-out',
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 2 }}>
        <Box sx={{ mt: 0.5 }}>{getIcon()}</Box>

        <Box sx={{ flexGrow: 1 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1, mb: 0.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Typography variant="h6" sx={{ fontSize: '1rem', fontWeight: notification.isRead ? 600 : 700 }}>
                {notification.title}
              </Typography>
              {!notification.isRead && (
                <Chip label="Nova" size="small" color="primary" sx={{ height: 20, fontSize: '0.65rem' }} />
              )}
            </Box>

            <Typography variant="caption" color="text.secondary">
              {new Date(notification.createdAt).toLocaleString('pt-BR')}
            </Typography>
          </Box>

          <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5, whiteSpace: 'pre-line' }}>
            {notification.message}
          </Typography>

          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Chip label={`Tipo: ${notification.type}`} size="small" variant="outlined" />
              <Chip
                label={`Prioridade: ${notification.priority}`}
                size="small"
                color={getPriorityColor() as any}
                variant="outlined"
              />
              {notification.dueDate && (
                <Typography variant="caption" color="warning.main" sx={{ fontWeight: 600 }}>
                  Vencimento: {new Date(notification.dueDate).toLocaleDateString('pt-BR')}
                </Typography>
              )}
            </Box>

            {!notification.isRead && (
              <Button
                size="small"
                variant="outlined"
                startIcon={<CheckCircle2 size={16} />}
                onClick={() => onMarkAsRead(notification.id)}
                disabled={isMarkingAsRead}
                sx={{ textTransform: 'none' }}
              >
                Marcar como lida
              </Button>
            )}
          </Box>
        </Box>
      </Box>
    </Paper>
  );
};
