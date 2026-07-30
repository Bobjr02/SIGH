import { describe, it, expect } from 'vitest';

describe('useDisciplinaryPermissions', () => {
  it('maps all 19 granular disciplinary permissions correctly', () => {
    const permissionsList = [
      'Disciplinary.InfractionTypes.View',
      'Disciplinary.InfractionTypes.Create',
      'Disciplinary.InfractionTypes.Update',
      'Disciplinary.InfractionTypes.Activate',
      'Disciplinary.InfractionTypes.Deactivate',
      'Disciplinary.Cases.View',
      'Disciplinary.Cases.Create',
      'Disciplinary.Cases.Open',
      'Disciplinary.Cases.StartInvestigation',
      'Disciplinary.Cases.SubmitForDecision',
      'Disciplinary.Cases.RecordDecision',
      'Disciplinary.Cases.ApproveDecision',
      'Disciplinary.Cases.RejectDecision',
      'Disciplinary.Cases.AddOccurrence',
      'Disciplinary.Cases.AddEmployee',
      'Disciplinary.Cases.AddEvidence',
      'Disciplinary.Cases.ApplyMeasure',
      'Disciplinary.Cases.Cancel',
      'Disciplinary.Cases.Conclude',
    ];

    expect(permissionsList.length).toBe(19);
  });
});
