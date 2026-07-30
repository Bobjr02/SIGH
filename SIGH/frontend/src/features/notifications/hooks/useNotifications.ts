import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationsApi } from '../api/notificationsApi';
import { notificationKeys } from '../keys/notificationKeys';
import { NotificationFilters } from '../types';

export const useGetNotifications = (filters: NotificationFilters) => {
  return useQuery({
    queryKey: notificationKeys.list(filters),
    queryFn: () => notificationsApi.getNotifications(filters),
  });
};

export const useGetUnreadNotifications = (companyId?: string) => {
  return useQuery({
    queryKey: notificationKeys.unread(companyId),
    queryFn: () => notificationsApi.getUnreadNotifications(companyId),
  });
};

export const useUnreadNotificationsCount = (pollingIntervalMs = 30000, companyId?: string) => {
  return useQuery({
    queryKey: notificationKeys.unreadCount(companyId),
    queryFn: () => notificationsApi.getUnreadCount(companyId),
    refetchInterval: pollingIntervalMs,
    staleTime: 10000,
  });
};

export const useMarkNotificationAsRead = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, companyId }: { id: string; companyId?: string }) =>
      notificationsApi.markAsRead(id, companyId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
};

export const useMarkAllNotificationsAsRead = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (companyId?: string | void) => notificationsApi.markAllAsRead(companyId || undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
};

export const useProcessDeadlines = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (companyId?: string | void) => notificationsApi.processDeadlines(companyId || undefined),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
};
