import { describe, it, expect } from 'vitest';
import { createDisciplinaryCaseSchema } from '../schemas';

describe('CreateDisciplinaryCasePage', () => {
  it('allows empty caseNumber so backend generates process number authoritatively', () => {
    const parsedBlank = createDisciplinaryCaseSchema.safeParse({
      caseNumber: '',
      companyId: '11111111-1111-1111-1111-111111111111',
      title: 'Processo de Teste',
    });
    expect(parsedBlank.success).toBe(true);
  });
});
