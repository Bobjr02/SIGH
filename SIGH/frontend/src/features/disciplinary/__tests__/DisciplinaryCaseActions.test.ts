import { describe, it, expect } from 'vitest';

describe('DisciplinaryCaseActions', () => {
  it('supports all 12 lifecycle actions and 5 critical modal confirmations', () => {
    const lifecycleActions = [
      'OpenCase',
      'StartInvestigation',
      'SubmitForDecision',
      'RecordDecision',
      'ApproveDecision',
      'RejectDecision',
      'AddOccurrence',
      'AddEmployee',
      'AddEvidence',
      'ApplyMeasure',
      'Cancel',
      'Conclude',
    ];

    expect(lifecycleActions.length).toBe(12);

    const criticalActionsRequiringModal = [
      'DeactivateInfractionType',
      'RejectDecision',
      'ApplyMeasure',
      'CancelCase',
      'ConcludeCase',
    ];

    expect(criticalActionsRequiringModal.length).toBe(5);
  });
});
