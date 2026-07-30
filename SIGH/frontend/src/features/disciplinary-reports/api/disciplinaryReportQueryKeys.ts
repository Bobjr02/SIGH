import {
  DashboardFilters,
  CaseReportFilters,
  MeasureReportFilters,
  EmployeeHistoryFilters,
} from '../types';

export const disciplinaryReportQueryKeys = {
  all: ['disciplinary-reports'] as const,

  dashboard: (filters?: DashboardFilters) =>
    [...disciplinaryReportQueryKeys.all, 'dashboard', filters ?? {}] as const,

  cases: (filters?: CaseReportFilters) =>
    [...disciplinaryReportQueryKeys.all, 'cases', filters ?? {}] as const,

  measures: (filters?: MeasureReportFilters) =>
    [...disciplinaryReportQueryKeys.all, 'measures', filters ?? {}] as const,

  employeeHistory: (employeeId: string, filters?: EmployeeHistoryFilters) =>
    [
      ...disciplinaryReportQueryKeys.all,
      'employee-history',
      employeeId,
      filters ?? {},
    ] as const,
};
