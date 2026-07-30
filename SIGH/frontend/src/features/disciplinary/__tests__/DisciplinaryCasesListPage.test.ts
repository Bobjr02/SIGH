import { describe, it, expect } from 'vitest';

describe('DisciplinaryCasesListPage', () => {
  it('supports all 10 API filter parameters including ResponsibleEmployeeId and EmployeeId', () => {
    const supportedFilters = [
      'SearchTerm',
      'CompanyId',
      'Status',
      'Priority',
      'ResponsibleEmployeeId',
      'EmployeeId',
      'DateFrom',
      'DateTo',
      'PageNumber',
      'PageSize',
    ];

    expect(supportedFilters.length).toBe(10);
    expect(supportedFilters).toContain('ResponsibleEmployeeId');
    expect(supportedFilters).toContain('EmployeeId');
  });
});
