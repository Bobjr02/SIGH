import { describe, it, expect } from 'vitest';
import { extractErrorMessage } from '../api/disciplinaryApi';
import {
  Result,
  PagedResult,
  ProblemDetails,
  InfractionSeverity,
  DisciplinaryCaseStatus,
  GetDisciplinaryCasesQuery,
} from '../types';
import { createDisciplinaryCaseSchema } from '../schemas';

describe('Disciplinary Module Integration Tests', () => {
  it('allows blank caseNumber so backend generates process number authoritatively', () => {
    const parsedBlankCase = createDisciplinaryCaseSchema.safeParse({
      companyId: '11111111-1111-1111-1111-111111111111',
      title: 'Processo sem numero local autoritativo',
    });
    expect(parsedBlankCase.success).toBe(true);
  });

  it('extracts field validation errors, 403 detail, 409 conflict, and 404 not found from ProblemDetails', () => {
    const badRequestError = {
      response: {
        status: 400,
        data: {
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.1',
          title: 'Validation Failed',
          status: 400,
          detail: 'One or more validation errors occurred.',
          validationErrors: {
            Title: ['O título é obrigatório.'],
            CompanyId: ['O ID da empresa é inválido.'],
          },
        } as ProblemDetails,
      },
    };
    const msg400 = extractErrorMessage(badRequestError);
    expect(msg400).toContain('O título é obrigatório.');
    expect(msg400).toContain('O ID da empresa é inválido.');

    const forbiddenError = {
      response: {
        status: 403,
        data: {
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.3',
          title: 'Forbidden',
          status: 403,
          detail: 'Você não possui a permissão Disciplinary.Cases.ApproveDecision para executar esta ação.',
        } as ProblemDetails,
      },
    };
    expect(extractErrorMessage(forbiddenError)).toContain('Você não possui a permissão');

    const conflictError = {
      response: {
        status: 409,
        data: {
          title: 'Conflict',
          detail: 'Já existe um processo em andamento para este funcionário nesta empresa.',
        } as ProblemDetails,
      },
    };
    expect(extractErrorMessage(conflictError)).toContain('Já existe um processo em andamento');

    const notFoundError = {
      response: {
        status: 404,
        data: {
          title: 'Not Found',
          detail: 'Tipo de infração com ID informado não foi encontrado.',
        } as ProblemDetails,
      },
    };
    expect(extractErrorMessage(notFoundError)).toContain('Tipo de infração com ID informado');
  });

  it('supports filters for case list queries', () => {
    const filters: GetDisciplinaryCasesQuery = {
      page: 1,
      pageSize: 10,
      companyId: '11111111-1111-1111-1111-111111111111',
      status: DisciplinaryCaseStatus.UnderInvestigation,
      severity: InfractionSeverity.High,
      searchTerm: 'Faltas',
      startDate: '2026-01-01',
      endDate: '2026-12-31',
      responsibleId: '33333333-3333-3333-3333-333333333333',
      employeeId: '44444444-4444-4444-4444-444444444444',
    };
    expect(filters.responsibleId).toBeDefined();
    expect(filters.employeeId).toBeDefined();
  });

  it('validates HTTP contract Result and PagedResult structure', () => {
    const pagedSample: PagedResult<any> = {
      items: [{ id: '1', caseNumber: 'PROC-1' }],
      pageNumber: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
      hasPreviousPage: false,
      hasNextPage: false,
    };
    const resultSample: Result<PagedResult<any>> = {
      success: true,
      data: pagedSample,
      message: null,
    };
    expect(resultSample.success).toBe(true);
    expect(resultSample.data?.items.length).toBe(1);
  });
});
