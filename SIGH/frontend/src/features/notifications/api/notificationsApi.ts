import api from '../../../services/api';
import {
  NotificationDto,
  NotificationFilters,
  PagedResult,
  ApiResult,
  DeadlineProcessingResultDto,
} from '../types';

export const notificationsApi = {
  getNotifications: async (filters: NotificationFilters): Promise<PagedResult<NotificationDto>> => {
    const response = await api.get<ApiResult<PagedResult<NotificationDto>>>('/v1/notifications', {
      params: {
        pageNumber: filters.pageNumber ?? 1,
        pageSize: filters.pageSize ?? 10,
        isRead: filters.isRead ?? undefined,
        type: filters.type || undefined,
        priority: filters.priority || undefined,
        startDate: filters.startDate || undefined,
        endDate: filters.endDate || undefined,
        sortBy: filters.sortBy || 'CreatedAt',
        sortDirection: filters.sortDirection || 'desc',
        companyId: filters.companyId || undefined,
      },
    });

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao buscar notificações.');
    }

    return response.data.data;
  },

  getUnreadNotifications: async (companyId?: string): Promise<NotificationDto[]> => {
    const response = await api.get<ApiResult<NotificationDto[]>>('/v1/notifications/unread', {
      params: { companyId },
    });

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao buscar notificações não lidas.');
    }

    return response.data.data;
  },

  getUnreadCount: async (companyId?: string): Promise<number> => {
    const response = await api.get<ApiResult<number>>('/v1/notifications/unread/count', {
      params: { companyId },
    });

    if (!response.data.success || typeof response.data.data !== 'number') {
      throw new Error(response.data.message || 'Erro ao obter contagem de não lidas.');
    }

    return response.data.data;
  },

  markAsRead: async (id: string, companyId?: string): Promise<NotificationDto> => {
    const response = await api.put<ApiResult<NotificationDto>>(`/v1/notifications/${id}/read`, null, {
      params: { companyId },
    });

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao marcar notificação como lida.');
    }

    return response.data.data;
  },

  markAllAsRead: async (companyId?: string): Promise<number> => {
    const response = await api.put<ApiResult<number>>('/v1/notifications/read-all', null, {
      params: { companyId },
    });

    if (!response.data.success || typeof response.data.data !== 'number') {
      throw new Error(response.data.message || 'Erro ao marcar todas como lidas.');
    }

    return response.data.data;
  },

  processDeadlines: async (companyId?: string): Promise<DeadlineProcessingResultDto> => {
    const response = await api.post<ApiResult<DeadlineProcessingResultDto>>('/v1/notifications/process-deadlines', null, {
      params: { companyId },
    });

    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao processar prazos.');
    }

    return response.data.data;
  },
};
