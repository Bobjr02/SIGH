import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  IconButton,
  Badge,
  Popover,
  Box,
  Typography,
  Button,
  List,
  ListItem,
  ListItemText,
  Divider,
  Chip,
  CircularProgress,
} from '@mui/material';
import { Bell, CheckCheck, ExternalLink } from 'lucide-react';
import {
  useUnreadNotificationsCount,
  useGetUnreadNotifications,
  useMarkNotificationAsRead,
  useMarkAllNotificationsAsRead,
} from '../hooks/useNotifications';
import { NotificationDto } from '../types';

export const NotificationBadge: React.FC = () => {
  const navigate = useNavigate();
  const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null);

  const { data: unreadCount = 0 } = useUnreadNotificationsCount(30000);
  const { data: unreadList = [], isLoading } = useGetUnreadNotifications();
  const markAsReadMutation = useMarkNotificationAsRead();
  const markAllAsReadMutation = useMarkAllNotificationsAsRead();

  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleMarkAsRead = async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    await markAsReadMutation.mutateAsync({ id });
  };

  const handleMarkAllAsRead = async () => {
    await markAllAsReadMutation.mutateAsync();
  };

  const handleViewAll = () => {
    handleClose();
    navigate('/notifications');
  };

  const open = Boolean(anchorEl);
  const id = open ? 'notification-popover' : undefined;

  const getTypeColor = (type: string) => {
    switch (type.toLowerCase()) {
      case 'escalation':
      case 'error':
        return 'error';
      case 'reminder':
      case 'warning':
        return 'warning';
      default:
        return 'info';
    }
  };

  return (
    <>
      <IconButton
        color="inherit"
        onClick={handleClick}
        aria-describedby={id}
        sx={{ ml: 1 }}
        data-testid="notification-bell-button"
      >
        <Badge badgeContent={unreadCount} color="error">
          <Bell size={20} />
        </Badge>
      </IconButton>

      <Popover
        id={id}
        open={open}
        anchorEl={anchorEl}
        onClose={handleClose}
        anchorOrigin={{
          vertical: 'bottom',
          horizontal: 'right',
        }}
        transformOrigin={{
          vertical: 'top',
          horizontal: 'right',
        }}
        slotProps={{
          paper: {
            sx: { width: 360, maxHeight: 480, display: 'flex', flexDirection: 'column' },
          },
        }}
      >
        <Box sx={{ p: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between', bgcolor: 'primary.main', color: 'primary.contrastText' }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
            Notificações ({unreadCount})
          </Typography>
          {unreadCount > 0 && (
            <Button
              size="small"
              color="inherit"
              startIcon={<CheckCheck size={14} />}
              onClick={handleMarkAllAsRead}
              disabled={markAllAsReadMutation.isPending}
              sx={{ textTransform: 'none', fontSize: '0.75rem' }}
            >
              Marcar todas
            </Button>
          )}
        </Box>

        <Divider />

        <Box sx={{ flexGrow: 1, overflowY: 'auto' }}>
          {isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', p: 3 }}>
              <CircularProgress size={24} />
            </Box>
          ) : unreadList.length === 0 ? (
            <Box sx={{ p: 3, textAlign: 'center' }}>
              <Typography variant="body2" color="text.secondary">
                Nenhuma notificação não lida no momento.
              </Typography>
            </Box>
          ) : (
            <List disablePadding>
              {unreadList.slice(0, 5).map((item: NotificationDto) => (
                <React.Fragment key={item.id}>
                  <ListItem
                    sx={{
                      display: 'flex',
                      flexDirection: 'column',
                      alignItems: 'flex-start',
                      bgcolor: item.isRead ? 'transparent' : 'action.hover',
                      py: 1.5,
                      px: 2,
                    }}
                  >
                    <Box sx={{ display: 'flex', width: '100%', justifyContent: 'space-between', alignItems: 'center', mb: 0.5 }}>
                      <Chip
                        label={item.type}
                        size="small"
                        color={getTypeColor(item.type) as any}
                        sx={{ height: 20, fontSize: '0.65rem' }}
                      />
                      <Typography variant="caption" color="text.secondary">
                        {new Date(item.createdAt).toLocaleDateString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
                      </Typography>
                    </Box>

                    <ListItemText
                      primary={item.title}
                      secondary={item.message}
                      slotProps={{
                        primary: { sx: { fontSize: '0.85rem', fontWeight: 600 } },
                        secondary: { sx: { fontSize: '0.75rem', color: 'text.secondary' } },
                      }}
                    />

                    <Box sx={{ alignSelf: 'flex-end', mt: 1 }}>
                      <Button
                        size="small"
                        variant="text"
                        onClick={(e) => handleMarkAsRead(item.id, e)}
                        disabled={markAsReadMutation.isPending}
                        sx={{ fontSize: '0.7rem', py: 0 }}
                      >
                        Marcar como lida
                      </Button>
                    </Box>
                  </ListItem>
                  <Divider />
                </React.Fragment>
              ))}
            </List>
          )}
        </Box>

        <Box sx={{ p: 1, borderTop: 1, borderColor: 'divider', textAlign: 'center' }}>
          <Button
            fullWidth
            size="small"
            endIcon={<ExternalLink size={14} />}
            onClick={handleViewAll}
            sx={{ textTransform: 'none', fontWeight: 600 }}
          >
            Ver todas as notificações
          </Button>
        </Box>
      </Popover>
    </>
  );
};
