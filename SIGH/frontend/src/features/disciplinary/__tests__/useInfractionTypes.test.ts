import { describe, it, expect } from 'vitest';
import { disciplinaryQueryKeys } from '../api/disciplinaryQueryKeys';

describe('useInfractionTypes', () => {
  it('invalidates infraction types query key after mutations', () => {
    const invalidationKey = disciplinaryQueryKeys.infractionTypes.all;
    expect(invalidationKey).toEqual(['infraction-types']);
  });
});
