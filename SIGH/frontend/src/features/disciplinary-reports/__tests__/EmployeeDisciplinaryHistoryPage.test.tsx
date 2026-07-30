import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { EmployeeDisciplinaryHistoryPage } from '../pages/EmployeeDisciplinaryHistoryPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../hooks/useDisciplinaryReportPermissions', () => ({
  useDisciplinaryReportPermissions: vi.fn(),
}));

vi.mock('../hooks/useDisciplinaryReports', () => ({
  useEmployeeDisciplinaryHistory: vi.fn(),
}));

import { useDisciplinaryReportPermissions } from '../hooks/useDisciplinaryReportPermissions';
import { useEmployeeDisciplinaryHistory } from '../hooks/useDisciplinaryReports';

describe('EmployeeDisciplinaryHistoryPage', () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <EmployeeDisciplinaryHistoryPage />
      </QueryClientProvider>
    );

  it('renders restricted access message when user lacks ViewEmployeeHistory permission', () => {
    vi.mocked(useDisciplinaryReportPermissions).mockReturnValue({
      canViewDashboard: false,
      canViewCases: false,
      canViewMeasures: false,
      canViewEmployeeHistory: false,
      canExport: false,
    });

    vi.mocked(useEmployeeDisciplinaryHistory).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as any);

    renderComponent();

    expect(screen.getByText('Acesso Restrito')).toBeDefined();
    expect(
      screen.getByText((content) =>
        content.includes('Você não possui permissão para consultar o histórico disciplinar do funcionário')
      )
    ).toBeDefined();
  });

  it('renders employee history timeline and metrics when data is present', () => {
    vi.mocked(useDisciplinaryReportPermissions).mockReturnValue({
      canViewDashboard: true,
      canViewCases: true,
      canViewMeasures: true,
      canViewEmployeeHistory: true,
      canExport: true,
    });

    vi.mocked(useEmployeeDisciplinaryHistory).mockReturnValue({
      data: {
        employeeId: '00000000-0000-0000-0000-000000000099',
        employeeName: 'Mariana Oliveira',
        registrationNumber: 'MAT-2026-99',
        companyId: '00000000-0000-0000-0000-000000000001',
        companyName: 'Empresa Matriz S/A',
        totalCases: 2,
        openCases: 1,
        concludedCases: 1,
        cancelledCases: 0,
        totalMeasures: 2,
        mostRecentCaseDate: '2026-01-10T00:00:00Z',
        cases: [
          {
            caseId: 'case-101',
            caseNumber: 'PROC-2026-101',
            title: 'Falta injustificada',
            status: 'Concluded',
            priority: 'Medium',
            openedAt: '2026-01-10T10:00:00Z',
            concludedAt: '2026-01-15T10:00:00Z',
            responsibleEmployee: 'Carlos Supervisor',
            occurrencesCount: 1,
            decisionsCount: 1,
            measuresCount: 1,
          },
        ],
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as any);

    renderComponent();

    expect(screen.getByText('Histórico Disciplinar do Funcionário')).toBeDefined();
    expect(screen.getByText('Mariana Oliveira')).toBeDefined();
    expect(screen.getByText('MAT-2026-99')).toBeDefined();
    expect(screen.getByText('PROC-2026-101')).toBeDefined();
    expect(screen.getByText('Falta injustificada')).toBeDefined();
  });
});
