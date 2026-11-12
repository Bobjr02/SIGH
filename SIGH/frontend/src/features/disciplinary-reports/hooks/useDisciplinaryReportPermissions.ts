import { useAuth } from '../../../hooks/useAuth';
import { PERMISSIONS } from '../../../auth/permissions';

export function useDisciplinaryReportPermissions() {
  const { hasPermission } = useAuth();

  return {
    canViewDashboard: hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_DASHBOARD),
    canViewCases: hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_CASES),
    canViewMeasures: hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES),
    canViewEmployeeHistory: hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY),
    canExport: hasPermission(PERMISSIONS.DISCIPLINARY_REPORTS_EXPORT),
  };
}
