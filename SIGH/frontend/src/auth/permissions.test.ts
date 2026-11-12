import { describe, expect, it } from 'vitest';
import permissionCodes from './permission-codes.json';
import { PERMISSIONS } from './permissions';

// permission-codes.json é comparado com PermissionCodes.All pelo teste do backend
// (PermissionCodesFrontendSyncTests). Aqui garantimos que as constantes TS espelham o JSON.
describe('PERMISSIONS', () => {
  it('lists exactly the codes of PermissionCodes.All, in the same order', () => {
    expect(Object.values(PERMISSIONS)).toEqual(permissionCodes);
  });

  it('has no duplicated codes', () => {
    const values = Object.values(PERMISSIONS);
    expect(new Set(values).size).toBe(values.length);
  });
});
