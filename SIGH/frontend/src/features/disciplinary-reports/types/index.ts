export interface DashboardDistributionItemDto {
  id?: string;
  label: string;
  value: number;
  percentage: number;
}

export interface DisciplinaryDashboardDto {
  totalCases: number;
  draftCases: number;
  openCases: number;
  underInvestigationCases: number;
  awaitingDecisionCases: number;
  decidedCases: number;
  concludedCases: number;
  cancelledCases: number;
  averageResolutionTimeInDays: number;
  casesOpenedInPeriod: number;
  casesConcludedInPeriod: number;
  employeesWithCases: number;
  measuresApplied: number;

  casesByStatus: DashboardDistributionItemDto[];
  casesByPriority: DashboardDistributionItemDto[];
  casesByMonth: DashboardDistributionItemDto[];
  casesByInfractionType: DashboardDistributionItemDto[];
  measuresByType: DashboardDistributionItemDto[];
  casesByDepartment: DashboardDistributionItemDto[];
}

export interface EmployeeDisciplinaryCaseSummaryDto {
  caseId: string;
  caseNumber: string;
  title: string;
  status: string;
  priority: string;
  openedAt: string;
  concludedAt?: string;
  responsibleEmployee: string;
  occurrencesCount: number;
  decisionsCount: number;
  measuresCount: number;
}

export interface EmployeeDisciplinaryHistoryDto {
  employeeId: string;
  employeeName: string;
  registrationNumber: string;
  companyId: string;
  companyName: string;
  totalCases: number;
  openCases: number;
  concludedCases: number;
  cancelledCases: number;
  totalMeasures: number;
  mostRecentCaseDate?: string;
  cases: EmployeeDisciplinaryCaseSummaryDto[];
}

export interface DisciplinaryMeasureSummaryDto {
  measureId: string;
  caseId: string;
  caseNumber: string;
  employeeId: string;
  employeeName: string;
  measureType: string;
  description: string;
  appliedAt: string;
  effectiveFrom?: string;
  effectiveTo?: string;
  appliedByEmployeeId: string;
  appliedByEmployeeName: string;
}

export interface DisciplinaryCaseReportItemDto {
  caseId: string;
  caseNumber: string;
  title: string;
  companyName: string;
  departmentName: string;
  status: string;
  priority: string;
  responsibleEmployeeName: string;
  mainAccusedEmployeeName: string;
  openedAt: string;
  concludedAt?: string;
  resolutionTimeInDays?: number;
  occurrencesCount: number;
  evidencesCount: number;
  decisionsCount: number;
  measuresCount: number;
}

export interface DashboardFilters {
  companyId?: string;
  dateFrom?: string;
  dateTo?: string;
  departmentId?: string;
  status?: string;
  priority?: string;
  responsibleEmployeeId?: string;
}

export interface CaseReportFilters {
  companyId?: string;
  departmentId?: string;
  employeeId?: string;
  responsibleEmployeeId?: string;
  infractionTypeId?: string;
  status?: string;
  priority?: string;
  dateFrom?: string;
  dateTo?: string;
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}

export interface MeasureReportFilters {
  companyId?: string;
  employeeId?: string;
  caseId?: string;
  measureType?: string;
  appliedFrom?: string;
  appliedTo?: string;
  searchTerm?: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface EmployeeHistoryFilters {
  companyId?: string;
  dateFrom?: string;
  dateTo?: string;
  status?: string;
  pageNumber?: number;
  pageSize?: number;
}
