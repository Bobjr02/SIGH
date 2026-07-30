import { describe, it, expect } from 'vitest';
import { disciplinaryQueryKeys } from '../api/disciplinaryQueryKeys';
import { GetDisciplinaryCasesQuery, GetInfractionTypesQuery, DisciplinaryCaseStatus } from '../types';

describe('disciplinaryQueryKeys', () => {
  it('generates correct query keys for cases', () => {
    expect(disciplinaryQueryKeys.cases.all).toEqual(['disciplinary-cases']);
    expect(disciplinaryQueryKeys.cases.lists()).toEqual(['disciplinary-cases', 'list']);

    const filters: GetDisciplinaryCasesQuery = {
      pageNumber: 1,
      pageSize: 10,
      status: DisciplinaryCaseStatus.UnderInvestigation,
    };

    expect(disciplinaryQueryKeys.cases.list(filters)).toEqual(['disciplinary-cases', 'list', filters]);
    expect(disciplinaryQueryKeys.cases.detail('case-123')).toEqual(['disciplinary-cases', 'detail', 'case-123']);
  });

  it('generates correct query keys for infraction types', () => {
    expect(disciplinaryQueryKeys.infractionTypes.all).toEqual(['infraction-types']);
    expect(disciplinaryQueryKeys.infractionTypes.lists()).toEqual(['infraction-types', 'list']);

    const infFilters: GetInfractionTypesQuery = { pageNumber: 1, pageSize: 10, isActive: true };
    expect(disciplinaryQueryKeys.infractionTypes.list(infFilters)).toEqual(['infraction-types', 'list', infFilters]);
    expect(disciplinaryQueryKeys.infractionTypes.detail('type-456')).toEqual(['infraction-types', 'detail', 'type-456']);
  });
});
