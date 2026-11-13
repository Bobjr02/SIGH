import { isAxiosError } from 'axios';
import { api } from '../../../services/api';
import type { PagedResult } from '../../employees/api/employeesApi';

interface Result<T> {
  success: boolean;
  data?: T;
  message?: string;
  errorCode?: string;
}

/** Espelha DisciplinaryMeasureType (só os tipos aceitos no registro). */
export enum MeasureType {
  VerbalWarning = 2,
  WrittenWarning = 3,
  Suspension = 4,
  JustCauseTermination = 7,
}

export const MEASURE_TYPE_LABELS: Record<number, string> = {
  [MeasureType.VerbalWarning]: 'Advertência verbal',
  [MeasureType.WrittenWarning]: 'Advertência escrita',
  [MeasureType.Suspension]: 'Suspensão',
  [MeasureType.JustCauseTermination]: 'Demissão por justa causa',
};

/** Espelha RecordStatus. */
export enum RecordStatus {
  PendingApproval = 1,
  Rejected = 2,
  Approved = 3,
  Cancelled = 4,
}

export const RECORD_STATUS_LABELS: Record<RecordStatus, string> = {
  [RecordStatus.PendingApproval]: 'Aguardando aprovação',
  [RecordStatus.Rejected]: 'Rejeitada',
  [RecordStatus.Approved]: 'Aprovada',
  [RecordStatus.Cancelled]: 'Cancelada',
};

export interface RecordSummary {
  id: string;
  caseNumber: string;
  status: RecordStatus;
  statusLabel: string;
  employeeId: string;
  employeeNumber: string;
  employeeName: string;
  managementUnitCode?: string | null;
  managementUnitName?: string | null;
  departmentName?: string | null;
  measureType: MeasureType;
  measureTypeLabel: string;
  infractionTypeId?: string | null;
  infractionTypeName?: string | null;
  measureDate?: string | null;
  firstOccurrenceDate?: string | null;
  suspensionDays?: number | null;
  registeredAt: string;
  registeredByUserId: string;
  registeredByName?: string | null;
}

export interface RecordEvidence {
  id: string;
  title: string;
  description?: string | null;
  evidenceType: string;
  hasFile: boolean;
  originalFileName?: string | null;
  contentType?: string | null;
  fileSize?: number | null;
  collectedAt: string;
  collectedByUserId: string;
}

export interface RecordHistoryItem {
  id: string;
  status: number;
  measureType?: number | null;
  decidedByName?: string | null;
  decidedAt: string;
  approvedByName?: string | null;
  approvedAt?: string | null;
  rejectedByName?: string | null;
  rejectedAt?: string | null;
  rejectionReason?: string | null;
}

export interface RecordActions {
  canApprove: boolean;
  canReject: boolean;
  canCorrect: boolean;
  canCancel: boolean;
  canAttach: boolean;
  canRemoveEvidence: boolean;
}

export interface RecordDetail {
  summary: RecordSummary;
  description: string;
  occurrenceDates: string[];
  suspensionStart?: string | null;
  suspensionEnd?: string | null;
  managementUnitCodeAtTime?: string | null;
  approvedAt?: string | null;
  approvedByName?: string | null;
  cancelledAt?: string | null;
  cancellationReason?: string | null;
  lastRejectionReason?: string | null;
  lastRejectedByName?: string | null;
  lastRejectedAt?: string | null;
  evidences: RecordEvidence[];
  history: RecordHistoryItem[];
  actions: RecordActions;
}

export interface RecordInput {
  employeeId: string;
  infractionTypeId: string;
  measureType: MeasureType;
  occurrenceDates: string[];
  measureDate: string;
  suspensionStart?: string | null;
  suspensionEnd?: string | null;
  description: string;
  confirmPossibleDuplicate?: boolean;
}

export interface PossibleDuplicate {
  id: string;
  caseNumber: string;
  status: RecordStatus;
  occurrenceDate: string;
}

export interface RecordSaveResult {
  record?: RecordDetail | null;
  possibleDuplicates: PossibleDuplicate[];
}

export interface Option {
  id: string;
  code?: string | null;
  name: string;
}

export interface RecordOptions {
  measureTypes: { value: MeasureType; label: string }[];
  infractionTypes: Option[];
  managementUnits: Option[];
  departments: Option[];
}

export interface RecordFilters {
  pageNumber?: number;
  pageSize?: number;
  status?: RecordStatus | '';
  measureType?: MeasureType | '';
  infractionTypeId?: string;
  managementUnitId?: string;
  departmentId?: string;
  from?: string;
  to?: string;
  employee?: string;
  mine?: boolean;
}

/** Aviso de possível duplicidade (409 POSSIBLE_DUPLICATE): exige confirmação. */
export class PossibleDuplicateError extends Error {
  constructor(public readonly duplicates: PossibleDuplicate[], message: string) {
    super(message);
  }
}

const clean = (filters: RecordFilters) =>
  Object.fromEntries(Object.entries(filters).filter(([, v]) => v !== undefined && v !== '' && v !== false));

const unwrap = <T,>(result: Result<T>, fallback: string): T => {
  if (!result.success || result.data === undefined) throw new Error(result.message || fallback);
  return result.data;
};

/** Mensagem do servidor (ProblemDetails.detail ou Result.message). */
export const recordErrorMessage = (error: unknown, fallback: string): string => {
  if (error instanceof PossibleDuplicateError) return error.message;
  if (isAxiosError<{ message?: string; detail?: string }>(error)) {
    if (error.response?.status === 404) return 'Medida não encontrada.';
    return error.response?.data?.detail || error.response?.data?.message || fallback;
  }
  return error instanceof Error ? error.message : fallback;
};

const save = async (request: () => Promise<{ data: Result<RecordSaveResult> }>): Promise<RecordDetail> => {
  try {
    const response = await request();
    return unwrap(response.data, 'Não foi possível salvar a medida.').record!;
  } catch (error) {
    if (isAxiosError<Result<RecordSaveResult>>(error) && error.response?.status === 409 && error.response.data?.errorCode === 'POSSIBLE_DUPLICATE') {
      throw new PossibleDuplicateError(error.response.data.data?.possibleDuplicates ?? [], error.response.data.message ?? 'Possível duplicidade.');
    }
    throw error;
  }
};

export const recordKeys = {
  all: ['records'] as const,
  list: (filters: RecordFilters) => ['records', 'list', clean(filters)] as const,
  detail: (id: string) => ['records', 'detail', id] as const,
  options: ['records', 'options'] as const,
  pendingCount: ['records', 'pending-count'] as const,
};

export const recordsApi = {
  async list(filters: RecordFilters): Promise<PagedResult<RecordSummary>> {
    const response = await api.get<Result<PagedResult<RecordSummary>>>('/v1/disciplinary-records', { params: clean(filters) });
    return unwrap(response.data, 'Erro ao listar as medidas.');
  },

  async get(id: string): Promise<RecordDetail> {
    const response = await api.get<Result<RecordDetail>>(`/v1/disciplinary-records/${id}`);
    return unwrap(response.data, 'Erro ao carregar a medida.');
  },

  async options(): Promise<RecordOptions> {
    const response = await api.get<Result<RecordOptions>>('/v1/disciplinary-records/options');
    return unwrap(response.data, 'Erro ao carregar as opções.');
  },

  async pendingCount(): Promise<number> {
    const response = await api.get<Result<number>>('/v1/disciplinary-records/pending-count');
    return unwrap(response.data, 'Erro ao contar as pendências.');
  },

  register: (input: RecordInput) => save(() => api.post<Result<RecordSaveResult>>('/v1/disciplinary-records', input)),

  correct: (id: string, input: RecordInput) => save(() => api.put<Result<RecordSaveResult>>(`/v1/disciplinary-records/${id}`, input)),

  async approve(id: string): Promise<RecordDetail> {
    const response = await api.post<Result<RecordDetail>>(`/v1/disciplinary-records/${id}/approve`);
    return unwrap(response.data, 'Não foi possível aprovar a medida.');
  },

  async reject(id: string, reason: string): Promise<RecordDetail> {
    const response = await api.post<Result<RecordDetail>>(`/v1/disciplinary-records/${id}/reject`, { reason });
    return unwrap(response.data, 'Não foi possível rejeitar a medida.');
  },

  async cancel(id: string, reason: string): Promise<RecordDetail> {
    const response = await api.post<Result<RecordDetail>>(`/v1/disciplinary-records/${id}/cancel`, { reason });
    return unwrap(response.data, 'Não foi possível cancelar a medida.');
  },

  /** Anexo = evidência do processo (metadados) + arquivo. */
  async createAttachment(recordId: string, evidenceType: number, title: string): Promise<string> {
    const response = await api.post<Result<{ id: string }>>(`/v1/disciplinary-cases/${recordId}/evidences`, {
      evidenceType,
      title,
      collectedAt: new Date().toISOString(),
    });
    return unwrap(response.data, 'Não foi possível registrar o anexo.').id;
  },
};

/** "2026-11-13" → "13/11/2026" (datas de negócio, sem fuso). */
export const formatBusinessDate = (value?: string | null): string => {
  if (!value) return '—';
  const [y, m, d] = value.slice(0, 10).split('-');
  return `${d}/${m}/${y}`;
};

/** Hoje no calendário local, "yyyy-MM-dd". */
export const todayIso = (): string => {
  const now = new Date();
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
};

/** Dias de suspensão, contando os dois extremos. */
export const suspensionDays = (start?: string | null, end?: string | null): number | null => {
  if (!start || !end) return null;
  const ms = Date.parse(`${end}T00:00:00Z`) - Date.parse(`${start}T00:00:00Z`);
  return Number.isNaN(ms) || ms < 0 ? null : ms / 86_400_000 + 1;
};
