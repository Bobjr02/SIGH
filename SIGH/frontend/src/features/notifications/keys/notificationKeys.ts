import { NotificationFilters } from '../types';

export const notificationKeys = {
  all: ['notifications'] as const,
  lists: () => [...notificationKeys.all, 'list'] as const,
  list: (filters: NotificationFilters) => [...notificationKeys.lists(), filters] as const,
  unread: (companyId?: string) => [...notificationKeys.all, 'unread', companyId ?? ''] as const,
  unreadCount: (companyId?: string) => [...notificationKeys.all, 'unread-count', companyId ?? ''] as const,
};
