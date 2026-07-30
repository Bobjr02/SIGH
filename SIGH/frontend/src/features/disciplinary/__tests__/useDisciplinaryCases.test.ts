import { describe, it, expect } from 'vitest';
import { disciplinaryQueryKeys } from '../api/disciplinaryQueryKeys';

describe('useDisciplinaryCases', () => {
  it('invalidates disciplinary cases keys after case lifecycle actions', () => {
    const caseId = '55555555-5555-5555-5555-555555555555';
    expect(disciplinaryQueryKeys.cases.all).toEqual(['disciplinary-cases']);
    expect(disciplinaryQueryKeys.cases.detail(caseId)).toEqual(['disciplinary-cases', 'detail', caseId]);
  });
});
