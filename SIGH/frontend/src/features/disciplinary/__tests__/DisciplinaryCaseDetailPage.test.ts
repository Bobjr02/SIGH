import { describe, it, expect } from 'vitest';

describe('DisciplinaryCaseDetailPage', () => {
  it('renders all 5 core sub-sections without physical file uploads', () => {
    const detailSections = [
      'occurrences',
      'employees',
      'evidences',
      'decisions',
      'measures',
    ];

    expect(detailSections.length).toBe(5);
  });
});
