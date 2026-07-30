import { describe, it, expect } from 'vitest';
import { mapReportQueryParams } from '../api/disciplinaryReportsApi';

describe('disciplinaryReportsApi', () => {
  it('should correctly map query parameters and strip null/undefined/empty values', () => {
    const inputParams = {
      companyId: 'comp-1',
      dateFrom: '2026-01-01',
      status: 'Opened',
      pageNumber: 1,
      pageSize: 10,
      emptyField: '',
      nullField: null,
      undefinedField: undefined,
    };

    const mapped = mapReportQueryParams(inputParams);

    expect(mapped.CompanyId).toBe('comp-1');
    expect(mapped.DateFrom).toBe('2026-01-01');
    expect(mapped.Status).toBe('Opened');
    expect(mapped.PageNumber).toBe(1);
    expect('EmptyField' in mapped).toBe(false);
    expect('NullField' in mapped).toBe(false);
    expect('UndefinedField' in mapped).toBe(false);
  });
});
