import { describe, it, expect } from 'vitest';
import {
  extractErrorMessage,
  mapDisciplinaryCasesQueryParams,
  mapInfractionTypesQueryParams,
} from '../api/disciplinaryApi';
import { GetDisciplinaryCasesQuery, GetInfractionTypesQuery, DisciplinaryCaseStatus, InfractionSeverity, ProblemDetails } from '../types';

describe('disciplinaryApi mapping and error extraction', () => {
  it('maps disciplinary cases query params correctly', () => {
    const caseFilters: GetDisciplinaryCasesQuery = {
      companyId: '11111111-1111-1111-1111-111111111111',
      status: DisciplinaryCaseStatus.UnderInvestigation,
      priority: InfractionSeverity.High,
      responsibleEmployeeId: '22222222-2222-2222-2222-222222222222',
      employeeId: '33333333-3333-3333-3333-333333333333',
      startDate: '2026-01-01',
      endDate: '2026-12-31',
      searchTerm: 'Infração grave',
      page: 2,
      pageSize: 20,
    };

    const mapped = mapDisciplinaryCasesQueryParams(caseFilters);
    expect(mapped?.CompanyId).toBe('11111111-1111-1111-1111-111111111111');
    expect(mapped?.Status).toBe(DisciplinaryCaseStatus.UnderInvestigation);
    expect(mapped?.Priority).toBe(InfractionSeverity.High);
    expect(mapped?.ResponsibleEmployeeId).toBe('22222222-2222-2222-2222-222222222222');
    expect(mapped?.EmployeeId).toBe('33333333-3333-3333-3333-333333333333');
    expect(mapped?.DateFrom).toBe('2026-01-01');
    expect(mapped?.DateTo).toBe('2026-12-31');
    expect(mapped?.SearchTerm).toBe('Infração grave');
    expect(mapped?.PageNumber).toBe(2);
    expect(mapped?.PageSize).toBe(20);
  });

  it('strips empty companyId from query params', () => {
    const emptyFilters: GetDisciplinaryCasesQuery = {
      companyId: '',
      searchTerm: '   ',
      page: 1,
      pageSize: 10,
    };
    const mappedEmpty = mapDisciplinaryCasesQueryParams(emptyFilters);
    expect('CompanyId' in (mappedEmpty || {})).toBe(false);
  });

  it('preserves boolean false for IsActive in infraction types mapping', () => {
    const typeFilters: GetInfractionTypesQuery = {
      companyId: '11111111-1111-1111-1111-111111111111',
      isActive: false,
      searchTerm: 'Leve',
      pageNumber: 1,
      pageSize: 10,
    };
    const mappedTypeParams = mapInfractionTypesQueryParams(typeFilters);
    expect(mappedTypeParams?.IsActive).toBe(false);
  });

  it('extracts error messages from ProblemDetails RFC 7807', () => {
    const err400 = {
      response: {
        status: 400,
        data: {
          title: 'Validation Failed',
          detail: 'General validation error message.',
          validationErrors: {
            Title: ['O título é obrigatório.'],
          },
        } as ProblemDetails,
      },
    };
    expect(extractErrorMessage(err400)).toBe('O título é obrigatório.');

    const err403 = {
      response: {
        status: 403,
        data: {
          title: 'Forbidden',
          detail: 'Você não possui a permissão Disciplinary.Cases.ApproveDecision.',
        } as ProblemDetails,
      },
    };
    expect(extractErrorMessage(err403)).toContain('Você não possui a permissão');
  });
});
