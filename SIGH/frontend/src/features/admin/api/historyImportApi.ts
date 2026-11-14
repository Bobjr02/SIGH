import { isAxiosError } from 'axios';
import { api } from '../../../services/api';
import type { PagedResult } from '../../employees/api/employeesApi';

interface Result<T> {
  success: boolean;
  data?: T;
  message?: string;
  errorCode?: string;
}

export interface HistorySummary {
  totalRows: number;
  toImport: number;
  existing: number;
  toUpdate: number;
  rejected: number;
  warnings: number;
  newInfractionTypes: number;
  absent: number;
}

export interface HistoryRow {
  row: number;
  employeeNumber: string;
  employeeName: string;
  date: string;
  measureType: number;
  measureTypeLabel: string;
  infractionName: string;
  newInfractionType: boolean;
  managementUnitCode?: string | null;
  managementUnitName: string;
  periodStart?: string | null;
  periodEnd?: string | null;
  suspensionDays?: number | null;
  daysUnknown: boolean;
}

export interface HistoryIssue {
  row?: number | null;
  employeeNumber?: string | null;
  severity: string;
  message: string;
}

export interface HistoryUpdate {
  row: HistoryRow;
  caseId: string;
  caseNumber: string;
  changes: { field: string; oldValue?: string | null; newValue?: string | null }[];
}

export interface HistoryAbsent {
  caseId: string;
  caseNumber: string;
  employeeNumber: string;
  date: string;
  measureTypeLabel: string;
  infractionName: string;
  importedAt?: string | null;
}

export interface HistoryPreview {
  batchId: string;
  fileName: string;
  fileSha256: string;
  expiresAt: string;
  summary: HistorySummary;
  toImport: HistoryRow[];
  existing: HistoryRow[];
  toUpdate: HistoryUpdate[];
  newInfractionTypes: string[];
  warnings: HistoryIssue[];
  rejected: HistoryIssue[];
  absent: HistoryAbsent[];
  fileWarnings: string[];
}

export interface HistoryResult {
  batchId: string;
  summary: HistorySummary;
  imported: number;
  updated: number;
  infractionTypesCreated: number;
  appliedAt: string;
}

export interface HistoryBatch {
  id: string;
  fileName: string;
  fileSha256: string;
  status: string;
  previewedAt: string;
  previewedByName?: string | null;
  appliedAt?: string | null;
  appliedByName?: string | null;
  summary: HistorySummary;
}

export interface HistoryStatus {
  isClosed: boolean;
  closedAt?: string | null;
  closedByName?: string | null;
}

const BASE = '/v1/disciplinary-history/import';

const unwrap = <T,>(result: Result<T>, fallback: string): T => {
  if (!result.success || result.data === undefined) throw new Error(result.message || fallback);
  return result.data;
};

/** Mensagem do servidor (inclusive o 409 de importação encerrada). */
export const historyErrorMessage = (error: unknown, fallback: string): string => {
  if (isAxiosError<{ message?: string; detail?: string }>(error))
    return error.response?.data?.message || error.response?.data?.detail || fallback;
  return error instanceof Error ? error.message : fallback;
};

const form = (file: File) => {
  const data = new FormData();
  data.append('file', file, file.name);
  return data;
};

export const historyKeys = {
  status: ['history-import', 'status'] as const,
  batches: ['history-import', 'batches'] as const,
};

export const historyImportApi = {
  async status(): Promise<HistoryStatus> {
    return unwrap((await api.get<Result<HistoryStatus>>(`${BASE}/status`)).data, 'Erro ao carregar o estado da importação.');
  },

  async batches(): Promise<PagedResult<HistoryBatch>> {
    return unwrap((await api.get<Result<PagedResult<HistoryBatch>>>(`${BASE}/batches`, { params: { pageSize: 20 } })).data, 'Erro ao carregar os lotes.');
  },

  async preview(file: File): Promise<HistoryPreview> {
    const response = await api.post<Result<HistoryPreview>>(`${BASE}/preview`, form(file), {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 0,
    });
    return unwrap(response.data, 'Erro na pré-visualização.');
  },

  /** Reenvia o MESMO arquivo: o servidor confere o SHA-256 e o plano. */
  async confirm(batchId: string, file: File): Promise<HistoryResult> {
    const response = await api.post<Result<HistoryResult>>(`${BASE}/${batchId}/confirm`, form(file), {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 0,
    });
    return unwrap(response.data, 'Erro ao confirmar a importação.');
  },

  async close(): Promise<HistoryStatus> {
    return unwrap((await api.post<Result<HistoryStatus>>(`${BASE}/close`)).data, 'Erro ao encerrar a importação.');
  },
};
