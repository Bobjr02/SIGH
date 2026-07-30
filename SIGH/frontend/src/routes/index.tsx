import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { Home } from '../pages/Home';
import { InfractionTypesPage } from '../features/disciplinary/pages/InfractionTypesPage';
import { DisciplinaryCasesListPage } from '../features/disciplinary/pages/DisciplinaryCasesListPage';
import { CreateDisciplinaryCasePage } from '../features/disciplinary/pages/CreateDisciplinaryCasePage';
import { DisciplinaryCaseDetailPage } from '../features/disciplinary/pages/DisciplinaryCaseDetailPage';
import { DisciplinaryDashboardPage } from '../features/disciplinary-reports/pages/DisciplinaryDashboardPage';
import { DisciplinaryCasesReportPage } from '../features/disciplinary-reports/pages/DisciplinaryCasesReportPage';
import { DisciplinaryMeasuresReportPage } from '../features/disciplinary-reports/pages/DisciplinaryMeasuresReportPage';
import { EmployeeDisciplinaryHistoryPage } from '../features/disciplinary-reports/pages/EmployeeDisciplinaryHistoryPage';
import { NotificationsPage } from '../features/notifications/pages/NotificationsPage';

export const AppRoutes: React.FC = () => {
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/notifications" element={<NotificationsPage />} />
      <Route path="/disciplinary/infraction-types" element={<InfractionTypesPage />} />
      <Route path="/disciplinary/cases" element={<DisciplinaryCasesListPage />} />
      <Route path="/disciplinary/cases/new" element={<CreateDisciplinaryCasePage />} />
      <Route path="/disciplinary/cases/:id" element={<DisciplinaryCaseDetailPage />} />
      <Route path="/disciplinary/reports/dashboard" element={<DisciplinaryDashboardPage />} />
      <Route path="/disciplinary/reports/cases" element={<DisciplinaryCasesReportPage />} />
      <Route path="/disciplinary/reports/measures" element={<DisciplinaryMeasuresReportPage />} />
      <Route path="/disciplinary/reports/employees/:employeeId/history" element={<EmployeeDisciplinaryHistoryPage />} />
      <Route path="/disciplinary/reports/employees/history" element={<EmployeeDisciplinaryHistoryPage />} />
      <Route path="*" element={<Navigate to="/disciplinary/cases" replace />} />
    </Routes>
  );
};
