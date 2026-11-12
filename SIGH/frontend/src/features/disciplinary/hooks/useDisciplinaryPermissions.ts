import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';

export const useDisciplinaryPermissions = () => {
  const { hasPermission, hasAnyPermission, permissions } = useAuth();

  return {
    // Permissões de Tipos de Infração
    canViewInfractionTypes: hasPermission(PERMISSIONS.DISCIPLINARY_INFRACTION_TYPES_VIEW),
    canCreateInfractionTypes: hasPermission(PERMISSIONS.DISCIPLINARY_INFRACTION_TYPES_CREATE),
    canUpdateInfractionTypes: hasPermission(PERMISSIONS.DISCIPLINARY_INFRACTION_TYPES_UPDATE),
    canActivateInfractionTypes: hasPermission(PERMISSIONS.DISCIPLINARY_INFRACTION_TYPES_ACTIVATE),
    canDeactivateInfractionTypes: hasPermission(PERMISSIONS.DISCIPLINARY_INFRACTION_TYPES_DEACTIVATE),

    // Permissões de Processos Disciplinares
    canViewCases: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_VIEW),
    canCreateCases: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_CREATE),
    canOpenCases: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_OPEN),
    canStartInvestigation: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_START_INVESTIGATION),
    canSubmitForDecision: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_SUBMIT_FOR_DECISION),
    canRecordDecision: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_RECORD_DECISION),
    canApproveDecision: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_APPROVE_DECISION),
    canRejectDecision: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_REJECT_DECISION),
    canAddOccurrence: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_ADD_OCCURRENCE),
    canAddEmployee: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_ADD_EMPLOYEE),
    canAddEvidence: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_ADD_EVIDENCE),
    canApplyMeasure: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_APPLY_MEASURE),
    canCancelCases: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_CANCEL),
    canConcludeCases: hasPermission(PERMISSIONS.DISCIPLINARY_CASES_CONCLUDE),

    hasPermission,
    hasAnyPermission,
    permissions,
  };
};
