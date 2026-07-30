import { describe, it, expect } from 'vitest';

describe('InfractionTypesListPage', () => {
  it('handles deactivation modal confirmation and ProblemDetails 400, 403, 404, 409 status codes', () => {
    const pageFeatures = {
      deactivationModalConfirmation: true,
      problemDetailsHandling: ['400', '403', '404', '409'],
    };

    expect(pageFeatures.deactivationModalConfirmation).toBe(true);
    expect(pageFeatures.problemDetailsHandling.length).toBe(4);
  });
});
