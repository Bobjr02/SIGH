import { useQuery, useMutation } from '@tanstack/react-query';
import { disciplinaryReportsApi } from '../api/disciplinaryReportsApi';
import { disciplinaryReportQueryKeys } from '../api/disciplinaryReportQueryKeys';
import {
  DashboardFilters,
  CaseReportFilters,
  MeasureReportFilters,
  EmployeeHistoryFilters,
} from '../types';

export function useDisciplinaryDashboard(filters?: DashboardFilters, enabled = true) {
  return useQuery({
    queryKey: disciplinaryReportQueryKeys.dashboard(filters),
    queryFn: () => disciplinaryReportsApi.getDashboard(filters),
    enabled,
    staleTime: 1000 * 60 * 5, // 5 minutes cache
  });
}

export function useDisciplinaryCaseReport(filters?: CaseReportFilters, enabled = true) {
  return useQuery({
    queryKey: disciplinaryReportQueryKeys.cases(filters),
    queryFn: () => disciplinaryReportsApi.getCasesReport(filters),
    enabled,
    staleTime: 1000 * 60 * 2, // 2 minutes cache
  });
}

export function useDisciplinaryMeasuresReport(filters?: MeasureReportFilters, enabled = true) {
  return useQuery({
    queryKey: disciplinaryReportQueryKeys.measures(filters),
    queryFn: () => disciplinaryReportsApi.getMeasuresReport(filters),
    enabled,
    staleTime: 1000 * 60 * 2,
  });
}

export function useEmployeeDisciplinaryHistory(
  employeeId: string,
  filters?: EmployeeHistoryFilters,
  enabled = true
) {
  return useQuery({
    queryKey: disciplinaryReportQueryKeys.employeeHistory(employeeId, filters),
    queryFn: () => disciplinaryReportsApi.getEmployeeHistory(employeeId, filters),
    enabled: enabled && Boolean(employeeId),
    staleTime: 1000 * 60 * 2,
  });
}

export function useExportCaseReportCsv() {
  return useMutation({
    mutationFn: async (filters?: CaseReportFilters) => {
      const blob = await disciplinaryReportsApi.exportCasesReportCsv(filters);
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `relatorio_processos_disciplinares_${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    },
  });
}
