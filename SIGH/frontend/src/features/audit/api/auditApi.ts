import { api } from '../../../services/api';

export interface AuditLogDto {
  id: string;
  createdAt: string;
  action: string;
  entity: string;
  entityId?: string | null;
  userId?: string | null;
  userName?: string | null;
  ipAddress?: string | null;
  correlationId?: string | null;
  /** JSON com os valores anteriores dos campos alterados. */
  oldValues?: string | null;
  /** JSON com os valores novos (ou os detalhes do evento de segurança). */
  newValues?: string | null;
}

export interface AuditPage {
  items: AuditLogDto[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface AuditLogFilters {
  entityName?: string;
  entityId?: string;
  userId?: string;
  action?: string;
  from?: string;
  to?: string;
  pageNumber?: number;
  pageSize?: number;
}

interface Result<T> {
  success: boolean;
  data?: T;
  message?: string;
}

const clean = (filters: AuditLogFilters) =>
  Object.fromEntries(Object.entries(filters).filter(([, v]) => v !== undefined && v !== null && v !== ''));

export const auditKeys = {
  all: ['audit-logs'] as const,
  search: (filters: AuditLogFilters) => ['audit-logs', 'search', clean(filters)] as const,
  caseTrail: (caseId: string, page: number) => ['audit-logs', 'case', caseId, page] as const,
};

export const auditApi = {
  async search(filters: AuditLogFilters): Promise<AuditPage> {
    const response = await api.get<Result<AuditPage>>('/v1/audit-logs', { params: clean(filters) });
    if (!response.data.success || !response.data.data) throw new Error(response.data.message || 'Erro ao consultar a auditoria.');
    return response.data.data;
  },

  async caseTrail(caseId: string, pageNumber = 1, pageSize = 100): Promise<AuditPage> {
    const response = await api.get<Result<AuditPage>>(`/v1/disciplinary-cases/${caseId}/audit-trail`, { params: { pageNumber, pageSize } });
    if (!response.data.success || !response.data.data) throw new Error(response.data.message || 'Erro ao carregar o histórico.');
    return response.data.data;
  },
};
