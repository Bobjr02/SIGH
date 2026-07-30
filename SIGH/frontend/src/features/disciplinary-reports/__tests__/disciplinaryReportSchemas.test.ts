import { describe, it, expect } from 'vitest';
import {
  dashboardFiltersSchema,
  caseReportFiltersSchema,
  measureReportFiltersSchema,
  employeeHistoryFiltersSchema,
} from '../schemas';

describe('disciplinaryReportSchemas', () => {
  it('should validate dashboard filters schema correctly', () => {
    const validDash = dashboardFiltersSchema.parse({
      companyId: '00000000-0000-0000-0000-000000000001',
      status: 'Opened',
    });
    expect(validDash.status).toBe('Opened');
  });

  it('should validate case report filters schema correctly', () => {
    const validCases = caseReportFiltersSchema.parse({
      pageNumber: 2,
      pageSize: 20,
      sortBy: 'OpenedAt',
      sortDirection: 'desc',
    });
    expect(validCases.pageSize).toBe(20);
    expect(validCases.sortDirection).toBe('desc');
  });

  it('should validate measure report filters schema correctly', () => {
    const validMeasures = measureReportFiltersSchema.parse({
      measureType: 'Suspension',
    });
    expect(validMeasures.measureType).toBe('Suspension');
  });

  it('should validate employee history filters schema correctly', () => {
    const validHistory = employeeHistoryFiltersSchema.parse({
      pageNumber: 1,
    });
    expect(validHistory.pageNumber).toBe(1);
  });
});
