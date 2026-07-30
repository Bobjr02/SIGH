import { useAuth } from '../../../hooks/useAuth';

export const useDisciplinaryPermissions = () => {
  const { hasPermission, hasAnyPermission, permissions, setPermissions, resetAllPermissions, clearAllPermissions } = useAuth();

  return {
    // Permissões de Tipos de Infração
    canViewInfractionTypes: hasPermission('Disciplinary.InfractionTypes.View') || hasPermission('Disciplinary.InfractionTypes.Read'),
    canCreateInfractionTypes: hasPermission('Disciplinary.InfractionTypes.Create'),
    canUpdateInfractionTypes: hasPermission('Disciplinary.InfractionTypes.Update'),
    canActivateInfractionTypes: hasPermission('Disciplinary.InfractionTypes.Activate'),
    canDeactivateInfractionTypes: hasPermission('Disciplinary.InfractionTypes.Deactivate'),

    // Permissões de Processos Disciplinares
    canViewCases: hasPermission('Disciplinary.Cases.View') || hasPermission('Disciplinary.Cases.Read'),
    canCreateCases: hasPermission('Disciplinary.Cases.Create'),
    canOpenCases: hasPermission('Disciplinary.Cases.Open'),
    canStartInvestigation: hasPermission('Disciplinary.Cases.StartInvestigation'),
    canSubmitForDecision: hasPermission('Disciplinary.Cases.SubmitForDecision'),
    canRecordDecision: hasPermission('Disciplinary.Cases.RecordDecision'),
    canApproveDecision: hasPermission('Disciplinary.Cases.ApproveDecision'),
    canRejectDecision: hasPermission('Disciplinary.Cases.RejectDecision'),
    canAddOccurrence: hasPermission('Disciplinary.Cases.AddOccurrence'),
    canAddEmployee: hasPermission('Disciplinary.Cases.AddEmployee'),
    canAddEvidence: hasPermission('Disciplinary.Cases.AddEvidence'),
    canApplyMeasure: hasPermission('Disciplinary.Cases.ApplyMeasure'),
    canCancelCases: hasPermission('Disciplinary.Cases.Cancel'),
    canConcludeCases: hasPermission('Disciplinary.Cases.Conclude'),

    hasPermission,
    hasAnyPermission,
    permissions,
    setPermissions,
    resetAllPermissions,
    clearAllPermissions,
  };
};
