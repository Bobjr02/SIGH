import api from '../../../services/api';
import {
  Result,
  PagedResult,
  InfractionTypeDto,
  CreateInfractionTypeRequest,
  CreateInfractionTypeResponse,
  UpdateInfractionTypeRequest,
  GetInfractionTypesQuery,
  DisciplinaryCaseSummaryDto,
  DisciplinaryCaseDetailDto,
  CreateDisciplinaryCaseRequest,
  CreateDisciplinaryCaseResponse,
  GetDisciplinaryCasesQuery,
  OpenCaseRequest,
  StartInvestigationRequest,
  SubmitCaseForDecisionRequest,
  RecordDecisionRequest,
  ApproveDecisionRequest,
  RejectDecisionRequest,
  AddOccurrenceRequest,
  AddEmployeeRequest,
  AddEvidenceRequest,
  ApplyMeasureRequest,
  CancelCaseRequest,
  ConcludeCaseRequest,
} from '../types';

export function extractErrorMessage(error: any): string {
  if (!error) return 'Ocorreu um erro inesperado.';

  if (error.response) {
    const data = error.response.data;
    if (data) {
      if (typeof data === 'string') return data;
      
      // 1. Validation errors by field
      if (data.validationErrors && typeof data.validationErrors === 'object') {
        const messages = Object.values(data.validationErrors).flat().filter(Boolean);
        if (messages.length > 0) return messages.join(' ');
      }
      if (data.errors) {
        if (Array.isArray(data.errors) && data.errors.length > 0) return data.errors.join(' ');
        if (typeof data.errors === 'object') {
          const messages = Object.values(data.errors).flat().filter(Boolean);
          if (messages.length > 0) return messages.join(' ');
        }
      }

      // 2. detail
      if (data.detail) return data.detail;

      // 3. title
      if (data.title) return data.title;

      // 4. message
      if (data.message) return data.message;
    }

    switch (error.response.status) {
      case 400:
        return 'Dados da requisição inválidos. Verifique os campos informados.';
      case 401:
        return 'Sessão expirada ou não autenticada. Faça login novamente.';
      case 403:
        return 'Acesso negado. Você não possui a permissão necessária para executar esta ação.';
      case 404:
        return 'O recurso solicitado não foi encontrado.';
      case 409:
        return 'Conflito de dados. Já existe um registro com as mesmas informações.';
      default:
        return `Erro no servidor (${error.response.status}).`;
    }
  }

  if (error.message) return error.message;
  return 'Erro de conexão com o servidor.';
}

export function mapDisciplinaryCasesQueryParams(filters?: GetDisciplinaryCasesQuery): Record<string, any> | undefined {
  if (!filters) return undefined;
  const rawParams: Record<string, any> = {
    CompanyId: filters.companyId || undefined,
    Status: filters.status !== undefined && filters.status !== null && (filters.status as any) !== 'all' ? filters.status : undefined,
    Priority: filters.priority !== undefined && filters.priority !== null && (filters.priority as any) !== 'all' ? filters.priority : (filters as any).severity !== undefined && (filters as any).severity !== 'all' ? (filters as any).severity : undefined,
    ResponsibleEmployeeId: filters.responsibleEmployeeId || filters.responsibleId || undefined,
    EmployeeId: filters.employeeId || undefined,
    DateFrom: filters.dateFrom || filters.startDate || undefined,
    DateTo: filters.dateTo || filters.endDate || undefined,
    SearchTerm: filters.searchTerm || undefined,
    PageNumber: filters.pageNumber || filters.page,
    PageSize: filters.pageSize,
  };

  const cleaned: Record<string, any> = {};
  Object.entries(rawParams).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') {
      cleaned[k] = v;
    }
  });

  return Object.keys(cleaned).length > 0 ? cleaned : undefined;
}

export function mapInfractionTypesQueryParams(filters?: GetInfractionTypesQuery): Record<string, any> | undefined {
  if (!filters) return undefined;
  const rawParams: Record<string, any> = {
    CompanyId: filters.companyId || undefined,
    IsActive: filters.isActive,
    SearchTerm: filters.searchTerm || undefined,
    PageNumber: filters.pageNumber || filters.page,
    PageSize: filters.pageSize,
  };

  const cleaned: Record<string, any> = {};
  Object.entries(rawParams).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') {
      cleaned[k] = v;
    }
  });

  return Object.keys(cleaned).length > 0 ? cleaned : undefined;
}

export const disciplinaryApi = {
  // --- TIPOS DE INFRAÇÃO ---
  async getInfractionTypes(params?: GetInfractionTypesQuery): Promise<PagedResult<InfractionTypeDto>> {
    const cleanedParams = mapInfractionTypesQueryParams(params);
    const response = await api.get<Result<PagedResult<InfractionTypeDto>>>('/v1/infraction-types', { params: cleanedParams });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao carregar tipos de infração.');
    }
    return response.data.data;
  },

  async getInfractionTypeById(id: string): Promise<InfractionTypeDto> {
    const response = await api.get<Result<InfractionTypeDto>>(`/v1/infraction-types/${id}`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao carregar detalhes do tipo de infração.');
    }
    return response.data.data;
  },

  async createInfractionType(request: CreateInfractionTypeRequest): Promise<CreateInfractionTypeResponse> {
    const response = await api.post<Result<CreateInfractionTypeResponse>>('/v1/infraction-types', request);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao criar tipo de infração.');
    }
    return response.data.data;
  },

  async updateInfractionType(id: string, request: UpdateInfractionTypeRequest): Promise<void> {
    const response = await api.put<Result<void>>(`/v1/infraction-types/${id}`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao atualizar tipo de infração.');
    }
  },

  async activateInfractionType(id: string): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/infraction-types/${id}/activate`);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao ativar tipo de infração.');
    }
  },

  async deactivateInfractionType(id: string): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/infraction-types/${id}/deactivate`);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao desativar tipo de infração.');
    }
  },

  // --- PROCESSOS DISCIPLINARES ---
  async getDisciplinaryCases(params?: GetDisciplinaryCasesQuery): Promise<PagedResult<DisciplinaryCaseSummaryDto>> {
    const cleanedParams = mapDisciplinaryCasesQueryParams(params);
    const response = await api.get<Result<PagedResult<DisciplinaryCaseSummaryDto>>>('/v1/disciplinary-cases', { params: cleanedParams });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao carregar processos disciplinares.');
    }
    return response.data.data;
  },

  async getDisciplinaryCaseById(id: string): Promise<DisciplinaryCaseDetailDto> {
    const response = await api.get<Result<{ caseDetail: DisciplinaryCaseDetailDto } | DisciplinaryCaseDetailDto>>(`/v1/disciplinary-cases/${id}`);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao carregar processo disciplinar.');
    }
    const data = response.data.data as any;
    return data.caseDetail || data;
  },

  async createDisciplinaryCase(request: CreateDisciplinaryCaseRequest): Promise<CreateDisciplinaryCaseResponse> {
    const response = await api.post<Result<CreateDisciplinaryCaseResponse>>('/v1/disciplinary-cases', request);
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao criar processo disciplinar.');
    }
    return response.data.data;
  },

  async openCase(id: string, request?: OpenCaseRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/open`, request || {});
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao abrir processo disciplinar.');
    }
  },

  async startInvestigation(id: string, request?: StartInvestigationRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/start-investigation`, request || {});
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao iniciar investigação.');
    }
  },

  async submitForDecision(id: string, request?: SubmitCaseForDecisionRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/submit-for-decision`, request || {});
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao submeter processo para decisão.');
    }
  },

  async recordDecision(id: string, request: RecordDecisionRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/decisions`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao registrar decisão.');
    }
  },

  async approveDecision(id: string, request?: ApproveDecisionRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/approve-decision`, request || {});
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao aprovar decisão.');
    }
  },

  async rejectDecision(id: string, request: RejectDecisionRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/reject-decision`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao rejeitar decisão.');
    }
  },

  async addOccurrence(id: string, request: AddOccurrenceRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/occurrences`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao adicionar ocorrência.');
    }
  },

  async addEmployee(id: string, request: AddEmployeeRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/employees`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao vincular funcionário.');
    }
  },

  async addEvidence(id: string, request: AddEvidenceRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/evidences`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao adicionar metadados da evidência.');
    }
  },

  async applyMeasure(id: string, request: ApplyMeasureRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/measures`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao aplicar medida disciplinar.');
    }
  },

  async cancelCase(id: string, request: CancelCaseRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/cancel`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao cancelar processo disciplinar.');
    }
  },

  async concludeCase(id: string, request: ConcludeCaseRequest): Promise<void> {
    const response = await api.post<Result<void>>(`/v1/disciplinary-cases/${id}/conclude`, request);
    if (!response.data.success) {
      throw new Error(response.data.message || 'Erro ao concluir processo disciplinar.');
    }
  },
};
