import React, { lazy, Suspense } from 'react';
import { Routes, Route, Navigate, Outlet } from 'react-router-dom';
import { ProtectedRoute, CHANGE_PASSWORD_PATH } from '../auth/ProtectedRoute';
import { RequirePermission } from '../auth/RequirePermission';
import { PERMISSIONS, type PermissionCode } from '../auth/permissions';
import { FullScreenLoader } from '../components/common/FullScreenLoader';
import { MainLayout } from '../layouts/MainLayout';

// Páginas carregadas sob demanda: cada uma vira um chunk separado do bundle principal.
const LoginPage = lazy(() => import('../pages/LoginPage'));
const ChangePasswordPage = lazy(() => import('../pages/ChangePasswordPage'));
const ForgotPasswordPage = lazy(() => import('../pages/ForgotPasswordPage'));
const ResetPasswordPage = lazy(() => import('../pages/ResetPasswordPage'));
const AuditPage = lazy(() => import('../features/audit/pages/AuditPage'));
const Home = lazy(() => import('../pages/Home').then((m) => ({ default: m.Home })));
const NotificationsPage = lazy(() =>
  import('../features/notifications/pages/NotificationsPage').then((m) => ({ default: m.NotificationsPage })),
);
const InfractionTypesPage = lazy(() =>
  import('../features/disciplinary/pages/InfractionTypesPage').then((m) => ({ default: m.InfractionTypesPage })),
);
const DisciplinaryCasesListPage = lazy(() =>
  import('../features/disciplinary/pages/DisciplinaryCasesListPage').then((m) => ({ default: m.DisciplinaryCasesListPage })),
);
const CreateDisciplinaryCasePage = lazy(() =>
  import('../features/disciplinary/pages/CreateDisciplinaryCasePage').then((m) => ({ default: m.CreateDisciplinaryCasePage })),
);
const DisciplinaryCaseDetailPage = lazy(() =>
  import('../features/disciplinary/pages/DisciplinaryCaseDetailPage').then((m) => ({ default: m.DisciplinaryCaseDetailPage })),
);
const DisciplinaryDashboardPage = lazy(() =>
  import('../features/disciplinary-reports/pages/DisciplinaryDashboardPage').then((m) => ({ default: m.DisciplinaryDashboardPage })),
);
const DisciplinaryCasesReportPage = lazy(() =>
  import('../features/disciplinary-reports/pages/DisciplinaryCasesReportPage').then((m) => ({ default: m.DisciplinaryCasesReportPage })),
);
const DisciplinaryMeasuresReportPage = lazy(() =>
  import('../features/disciplinary-reports/pages/DisciplinaryMeasuresReportPage').then((m) => ({
    default: m.DisciplinaryMeasuresReportPage,
  })),
);
const EmployeeDisciplinaryHistoryPage = lazy(() =>
  import('../features/disciplinary-reports/pages/EmployeeDisciplinaryHistoryPage').then((m) => ({
    default: m.EmployeeDisciplinaryHistoryPage,
  })),
);

const guard = (permission: PermissionCode, element: React.ReactNode) => (
  <RequirePermission permission={permission}>{element}</RequirePermission>
);

const AppLayout: React.FC = () => (
  <MainLayout>
    <Suspense fallback={<FullScreenLoader />}>
      <Outlet />
    </Suspense>
  </MainLayout>
);

export const AppRoutes: React.FC = () => {
  return (
    <Suspense fallback={<FullScreenLoader />}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        {/* Públicas: recuperação de senha e definição no primeiro acesso. */}
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />

        <Route element={<ProtectedRoute />}>
          <Route path={CHANGE_PASSWORD_PATH} element={<ChangePasswordPage />} />

          <Route element={<AppLayout />}>
            <Route path="/" element={<Home />} />
            <Route path="/notifications" element={guard(PERMISSIONS.NOTIFICATIONS_VIEW, <NotificationsPage />)} />
            <Route
              path="/disciplinary/infraction-types"
              element={guard(PERMISSIONS.DISCIPLINARY_INFRACTION_TYPES_VIEW, <InfractionTypesPage />)}
            />
            <Route path="/disciplinary/cases" element={guard(PERMISSIONS.DISCIPLINARY_CASES_VIEW, <DisciplinaryCasesListPage />)} />
            <Route
              path="/disciplinary/cases/new"
              element={guard(PERMISSIONS.DISCIPLINARY_CASES_CREATE, <CreateDisciplinaryCasePage />)}
            />
            <Route path="/disciplinary/cases/:id" element={guard(PERMISSIONS.DISCIPLINARY_CASES_VIEW, <DisciplinaryCaseDetailPage />)} />
            <Route
              path="/disciplinary/reports/dashboard"
              element={guard(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_DASHBOARD, <DisciplinaryDashboardPage />)}
            />
            <Route
              path="/disciplinary/reports/cases"
              element={guard(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_CASES, <DisciplinaryCasesReportPage />)}
            />
            <Route
              path="/disciplinary/reports/measures"
              element={guard(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES, <DisciplinaryMeasuresReportPage />)}
            />
            <Route
              path="/disciplinary/reports/employees/:employeeId/history"
              element={guard(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY, <EmployeeDisciplinaryHistoryPage />)}
            />
            <Route
              path="/disciplinary/reports/employees/history"
              element={guard(PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY, <EmployeeDisciplinaryHistoryPage />)}
            />
            <Route path="/audit" element={guard(PERMISSIONS.AUDIT_VIEW, <AuditPage />)} />
            <Route path="*" element={<Navigate to="/disciplinary/cases" replace />} />
          </Route>
        </Route>
      </Routes>
    </Suspense>
  );
};
