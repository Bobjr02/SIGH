import { useAuth } from '../../../hooks/useAuth';

export function useDisciplinaryReportPermissions() {
  const { hasPermission } = useAuth();

  return {
    canViewDashboard: hasPermission('Disciplinary.Reports.ViewDashboard'),
    canViewCases: hasPermission('Disciplinary.Reports.ViewCases'),
    canViewMeasures: hasPermission('Disciplinary.Reports.ViewMeasures'),
    canViewEmployeeHistory: hasPermission('Disciplinary.Reports.ViewEmployeeHistory'),
    canExport: hasPermission('Disciplinary.Reports.Export'),
  };
}
