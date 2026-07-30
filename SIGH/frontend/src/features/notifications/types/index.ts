export type NotificationPriority = 'Low' | 'Medium' | 'High' | 'Critical' | string;
export type NotificationType = 'Info' | 'Reminder' | 'Escalation' | 'Warning' | 'Error' | string;

export interface NotificationDto {
  id: string;
  companyId: string;
  recipientUserId: string;
  recipientEmail?: string;
  title: string;
  message: string;
  type: NotificationType;
  priority: NotificationPriority;
  createdAt: string;
  isRead: boolean;
  readAt?: string | null;
  isExpired: boolean;
  dueDate?: string | null;
  metadata?: string | null;
}

export interface NotificationFilters {
  pageNumber?: number;
  pageSize?: number;
  isRead?: boolean | null;
  type?: string | null;
  priority?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  companyId?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalItems: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ApiResult<T> {
  success: boolean;
  message?: string;
  errorCode?: string;
  validationErrors?: Record<string, string[]>;
  data?: T;
}

export interface DeadlineProcessingResultDto {
  processedCasesCount: number;
  remindersSentCount: number;
  escalationsProcessedCount: number;
  details: string[];
}
