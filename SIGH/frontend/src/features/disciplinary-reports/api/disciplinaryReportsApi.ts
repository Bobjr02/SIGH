import api from '../../../services/api';
import { Result, PagedResult } from '../../disciplinary/types';
import {
  DisciplinaryDashboardDto,
  DisciplinaryCaseReportItemDto,
  DisciplinaryMeasureSummaryDto,
  EmployeeDisciplinaryHistoryDto,
  DashboardFilters,
  CaseReportFilters,
  MeasureReportFilters,
  EmployeeHistoryFilters,
} from '../types';

export function mapReportQueryParams(params?: Record<string, any>): Record<string, any> {
  if (!params) return {};
  const query: Record<string, any> = {};

  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      // Capitalize first letter for C# ASP.NET Core Query compatibility
      const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
      query[pascalKey] = value;
    }
  });

  return query;
}

export const disciplinaryReportsApi = {
  getDashboard: async (filters?: DashboardFilters): Promise<DisciplinaryDashboardDto> => {
    const params = mapReportQueryParams(filters);
    const response = await api.get<Result<DisciplinaryDashboardDto>>('/api/v1/disciplinary-reports/dashboard', { params });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao carregar os indicadores do dashboard.');
    }
    return response.data.data;
  },

  getCasesReport: async (filters?: CaseReportFilters): Promise<PagedResult<DisciplinaryCaseReportItemDto>> => {
    const params = mapReportQueryParams(filters);
    const response = await api.get<Result<PagedResult<DisciplinaryCaseReportItemDto>>>('/api/v1/disciplinary-reports/cases', { params });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao carregar o relatório de processos.');
    }
    return response.data.data;
  },

  exportCasesReportCsv: async (filters?: CaseReportFilters): Promise<Blob> => {
    const params = mapReportQueryParams(filters);
    const response = await api.get('/api/v1/disciplinary-reports/cases/export', {
      params,
      responseType: 'blob',
    });
    return response.data;
  },

  getMeasuresReport: async (filters?: MeasureReportFilters): Promise<PagedResult<DisciplinaryMeasureSummaryDto>> => {
    const params = mapReportQueryParams(filters);
    const response = await api.get<Result<PagedResult<DisciplinaryMeasureSummaryDto>>>('/api/v1/disciplinary-reports/measures', { params });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao carregar o relatório de medidas.');
    }
    return response.data.data;
  },

  getEmployeeHistory: async (employeeId: string, filters?: EmployeeHistoryFilters): Promise<EmployeeDisciplinaryHistoryDto> => {
    const params = mapReportQueryParams(filters);
    const response = await api.get<Result<EmployeeDisciplinaryHistoryDto>>(`/api/v1/disciplinary-reports/employees/${employeeId}/history`, { params });
    if (!response.data.success || !response.data.data) {
      throw new Error(response.data.message || 'Erro ao carregar o histórico disciplinar do funcionário.');
    }
    return response.data.data;
  },
};
