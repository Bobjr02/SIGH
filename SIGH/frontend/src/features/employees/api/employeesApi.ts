import { api } from '../../../services/api';

interface Result<T> {
  success: boolean;
  data?: T;
  message?: string;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Espelha EmployeeStatus do backend. */
export enum EmployeeStatus {
  PendingAdmission = 1,
  Active = 2,
  OnLeave = 3,
  Inactive = 4,
  Terminated = 5,
}

/** Espelha EmployeeGroup do backend. */
export enum EmployeeGroup {
  Field = 1,
  Industry = 2,
}

export const STATUS_LABELS: Record<EmployeeStatus, string> = {
  [EmployeeStatus.PendingAdmission]: 'Admissão pendente',
  [EmployeeStatus.Active]: 'Ativo',
  [EmployeeStatus.OnLeave]: 'Afastado',
  [EmployeeStatus.Inactive]: 'Inativo',
  [EmployeeStatus.Terminated]: 'Desligado',
};

export const GROUP_LABELS: Record<EmployeeGroup, string> = {
  [EmployeeGroup.Field]: 'Campo',
  [EmployeeGroup.Industry]: 'Indústria',
};

export interface EmployeeListItem {
  id: string;
  employeeNumber: string;
  fullName: string;
  /** Sempre mascarado pelo servidor (***.982.247-**). */
  cpfMasked: string;
  admissionDate: string;
  status: EmployeeStatus;
  jobTitleName?: string | null;
  managementUnitName?: string | null;
  managementUnitCode?: string | null;
  departmentName?: string | null;
  departmentCode?: string | null;
  group?: EmployeeGroup | null;
  team?: string | null;
}

export interface EmployeeDetail extends EmployeeListItem {
  /** CPF completo: só no detalhe. */
  cpf?: string | null;
  jobTitleCode?: string | null;
  terminationDate?: string | null;
  terminationReason?: string | null;
  companyName?: string | null;
}

export interface OrganizationOption {
  id: string;
  code?: string | null;
  name: string;
  managementUnitId?: string | null;
}

export interface FilterOptions {
  managementUnits: OrganizationOption[];
  departments: OrganizationOption[];
}

export interface EmployeeListFilters {
  searchTerm?: string;
  status?: EmployeeStatus | '';
  group?: EmployeeGroup | '';
  managementUnitId?: string;
  departmentId?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface CountByLabel {
  label: string;
  count: number;
}

export interface DecisionSupport {
  employeeId: string;
  totalMeasures: number;
  warnings: number;
  verbalWarnings: number;
  writtenWarnings: number;
  suspensions: number;
  suspensionDays: number;
  suspensionsWithoutPeriod: number;
  otherMeasures: number;
  byType: CountByLabel[];
  byInfractionType: CountByLabel[];
  lastMeasureAt?: string | null;
  measuresLast6Months: number;
  measuresLast12Months: number;
  sources: string[];
  /** "2 advertências — 1 histórica, 1 no SIGH". */
  byOrigin?: { label: string; total: number; historical: number; sigh: number }[];
  historicalMeasures?: number;
  sighMeasures?: number;
}

export interface FieldChange {
  field: string;
  oldValue?: string | null;
  newValue?: string | null;
}

export interface ImportChange {
  employeeNumber: string;
  fullName: string;
  changes: FieldChange[];
}

export interface ImportIssue {
  row?: number | null;
  employeeNumber?: string | null;
  severity: string;
  message: string;
}

export interface ImportSummary {
  totalRows: number;
  created: number;
  updated: number;
  terminated: number;
  terminatedByAbsence: number;
  reactivated: number;
  unchanged: number;
  errors: number;
  warnings: number;
}

export interface ImportPreview {
  batchId: string;
  fileName: string;
  fileSha256: string;
  expiresAt: string;
  summary: ImportSummary;
  created: ImportChange[];
  updated: ImportChange[];
  toTerminate: ImportChange[];
  reactivated: ImportChange[];
  unchanged: string[];
  errors: ImportIssue[];
  warnings: ImportIssue[];
  newManagementUnits: string[];
  activeBefore: number;
  absenceTerminationPercent: number;
  maxTerminationPercent: number;
  requiresMassTerminationConfirmation: boolean;
}

export interface ImportResult {
  batchId: string;
  summary: ImportSummary;
  appliedAt: string;
}

export interface ImportBatch {
  id: string;
  fileName: string;
  fileSha256: string;
  status: 'Previewed' | 'Applied';
  previewedAt: string;
  previewedByName?: string | null;
  appliedAt?: string | null;
  appliedByName?: string | null;
  summary: ImportSummary;
  massTerminationConfirmed: boolean;
}

const unwrap = <T,>(result: Result<T>, fallback: string): T => {
  if (!result.success || result.data === undefined) throw new Error(result.message || fallback);
  return result.data;
};

const clean = (filters: EmployeeListFilters) =>
  Object.fromEntries(Object.entries(filters).filter(([, v]) => v !== undefined && v !== null && v !== ''));

const xlsxForm = (file: File, extra?: Record<string, string>) => {
  const form = new FormData();
  form.append('file', file, file.name);
  Object.entries(extra ?? {}).forEach(([k, v]) => form.append(k, v));
  return form;
};

export const employeeKeys = {
  all: ['employees'] as const,
  list: (filters: EmployeeListFilters) => ['employees', 'list', clean(filters)] as const,
  detail: (id: string) => ['employees', 'detail', id] as const,
  decisionSupport: (id: string) => ['employees', 'decision-support', id] as const,
  filterOptions: ['employees', 'filter-options'] as const,
  importBatches: ['employees', 'import-batches'] as const,
};

export const employeesApi = {
  async list(filters: EmployeeListFilters): Promise<PagedResult<EmployeeListItem>> {
    const response = await api.get<Result<PagedResult<EmployeeListItem>>>('/v1/employees', { params: clean(filters) });
    return unwrap(response.data, 'Erro ao listar funcionários.');
  },

  async getById(id: string): Promise<EmployeeDetail> {
    const response = await api.get<Result<EmployeeDetail>>(`/v1/employees/${id}`);
    return unwrap(response.data, 'Erro ao carregar o funcionário.');
  },

  async filterOptions(): Promise<FilterOptions> {
    const response = await api.get<Result<FilterOptions>>('/v1/employees/filter-options');
    return unwrap(response.data, 'Erro ao carregar os filtros.');
  },

  async decisionSupport(id: string): Promise<DecisionSupport> {
    const response = await api.get<Result<DecisionSupport>>(`/v1/employees/${id}/decision-support`);
    return unwrap(response.data, 'Erro ao carregar o quadro de apoio à decisão.');
  },

  /** Dry-run: nada é gravado. */
  async previewImport(file: File): Promise<ImportPreview> {
    const response = await api.post<Result<ImportPreview>>('/v1/employees/import/preview', xlsxForm(file), {
      headers: { 'Content-Type': 'multipart/form-data' },
      timeout: 0,
    });
    return unwrap(response.data, 'Erro na pré-visualização.');
  },

  /** Reenvia o MESMO arquivo: o servidor confere o SHA-256 do lote. */
  async confirmImport(batchId: string, file: File, confirmMassTermination: boolean): Promise<ImportResult> {
    const response = await api.post<Result<ImportResult>>(
      `/v1/employees/import/${batchId}/confirm`,
      xlsxForm(file, { confirmMassTermination: confirmMassTermination ? 'true' : 'false' }),
      { headers: { 'Content-Type': 'multipart/form-data' }, timeout: 0 },
    );
    return unwrap(response.data, 'Erro ao confirmar a importação.');
  },

  async importBatches(): Promise<PagedResult<ImportBatch>> {
    const response = await api.get<Result<PagedResult<ImportBatch>>>('/v1/employees/import/batches', { params: { pageSize: 20 } });
    return unwrap(response.data, 'Erro ao carregar o histórico de importações.');
  },
};

/** Garante a máscara mesmo se um valor completo chegar por engano: ***.982.247-** */
export const maskCpf = (value?: string | null): string => {
  const digits = (value ?? '').replace(/\D/g, '');
  if (digits.length !== 11) return value && value.includes('*') ? value : '***.***.***-**';
  return `***.${digits.slice(3, 6)}.${digits.slice(6, 9)}-**`;
};

export const formatCpf = (value?: string | null): string => {
  const d = (value ?? '').replace(/\D/g, '');
  return d.length === 11 ? `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}` : (value ?? '-');
};

export const formatDate = (iso?: string | null) => {
  if (!iso) return '-';
  const [y, m, d] = iso.slice(0, 10).split('-');
  return y && m && d ? `${d}/${m}/${y}` : iso;
};
