import { GetDisciplinaryCasesQuery, GetInfractionTypesQuery } from '../types';

export const disciplinaryQueryKeys = {
  cases: {
    all: ['disciplinary-cases'] as const,
    lists: () => [...disciplinaryQueryKeys.cases.all, 'list'] as const,
    list: (filters?: GetDisciplinaryCasesQuery) =>
      [...disciplinaryQueryKeys.cases.lists(), filters || {}] as const,
    details: () => [...disciplinaryQueryKeys.cases.all, 'detail'] as const,
    detail: (id: string) =>
      [...disciplinaryQueryKeys.cases.details(), id] as const,
  },

  infractionTypes: {
    all: ['infraction-types'] as const,
    lists: () => [...disciplinaryQueryKeys.infractionTypes.all, 'list'] as const,
    list: (filters?: GetInfractionTypesQuery) =>
      [...disciplinaryQueryKeys.infractionTypes.lists(), filters || {}] as const,
    details: () => [...disciplinaryQueryKeys.infractionTypes.all, 'detail'] as const,
    detail: (id: string) =>
      [...disciplinaryQueryKeys.infractionTypes.details(), id] as const,
  },
};
