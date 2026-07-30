import { describe, it, expect } from 'vitest';
import { disciplinaryReportQueryKeys } from '../api/disciplinaryReportQueryKeys';

describe('disciplinaryReportQueryKeys', () => {
  it('should generate correct root key', () => {
    const root = disciplinaryReportQueryKeys.all;
    expect(root[0]).toBe('disciplinary-reports');
  });

  it('should generate correct dashboard query key', () => {
    const dashKey = disciplinaryReportQueryKeys.dashboard({ companyId: 'comp-1' });
    expect(dashKey[0]).toBe('disciplinary-reports');
    expect(dashKey[1]).toBe('dashboard');
    expect((dashKey[2] as Record<string, string>).companyId).toBe('comp-1');
  });

  it('should generate correct cases report query key', () => {
    const caseKey = disciplinaryReportQueryKeys.cases({ status: 'Opened' as any });
    expect(caseKey[0]).toBe('disciplinary-reports');
    expect(caseKey[1]).toBe('cases');
    expect((caseKey[2] as Record<string, string>).status).toBe('Opened');
  });

  it('should generate correct measures report query key', () => {
    const measureKey = disciplinaryReportQueryKeys.measures({ measureType: 'Suspension' });
    expect(measureKey[0]).toBe('disciplinary-reports');
    expect(measureKey[1]).toBe('measures');
    expect((measureKey[2] as Record<string, string>).measureType).toBe('Suspension');
  });

  it('should generate correct employee history query key', () => {
    const historyKey = disciplinaryReportQueryKeys.employeeHistory('emp-123');
    expect(historyKey[0]).toBe('disciplinary-reports');
    expect(historyKey[1]).toBe('employee-history');
    expect(historyKey[2]).toBe('emp-123');
  });
});
