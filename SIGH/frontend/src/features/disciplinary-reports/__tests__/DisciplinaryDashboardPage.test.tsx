import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DisciplinaryDashboardPage } from '../pages/DisciplinaryDashboardPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../hooks/useDisciplinaryReportPermissions', () => ({
  useDisciplinaryReportPermissions: vi.fn(),
}));

vi.mock('../hooks/useDisciplinaryReports', () => ({
  useDisciplinaryDashboard: vi.fn(),
}));

import { useDisciplinaryReportPermissions } from '../hooks/useDisciplinaryReportPermissions';
import { useDisciplinaryDashboard } from '../hooks/useDisciplinaryReports';

describe('DisciplinaryDashboardPage', () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <DisciplinaryDashboardPage />
      </QueryClientProvider>
    );

  it('renders restricted access message when user lacks ViewDashboard permission', () => {
    vi.mocked(useDisciplinaryReportPermissions).mockReturnValue({
      canViewDashboard: false,
      canViewCases: false,
      canViewMeasures: false,
      canViewEmployeeHistory: false,
      canExport: false,
    });

    vi.mocked(useDisciplinaryDashboard).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as any);

    renderComponent();

    expect(screen.getByText('Acesso Restrito')).toBeDefined();
    expect(
      screen.getByText((content) =>
        content.includes('Você não possui permissão para visualizar o dashboard disciplinar')
      )
    ).toBeDefined();
  });

  it('renders KPI indicators when permissions are present and data loads', () => {
    vi.mocked(useDisciplinaryReportPermissions).mockReturnValue({
      canViewDashboard: true,
      canViewCases: true,
      canViewMeasures: true,
      canViewEmployeeHistory: true,
      canExport: true,
    });

    vi.mocked(useDisciplinaryDashboard).mockReturnValue({
      data: {
        totalCases: 15,
        draftCases: 2,
        openCases: 5,
        underInvestigationCases: 3,
        awaitingDecisionCases: 1,
        decidedCases: 1,
        concludedCases: 3,
        cancelledCases: 0,
        averageResolutionTimeInDays: 4.5,
        casesOpenedInPeriod: 15,
        casesConcludedInPeriod: 3,
        employeesWithCases: 8,
        measuresApplied: 10,
        casesByStatus: [{ label: 'Opened', value: 5, percentage: 33.33 }],
        casesByPriority: [{ label: 'High', value: 4, percentage: 26.67 }],
        casesByMonth: [{ label: '2026-01', value: 15, percentage: 100 }],
        casesByInfractionType: [{ label: 'Atraso', value: 6, percentage: 40 }],
        measuresByType: [{ label: 'Warning', value: 7, percentage: 70 }],
        casesByDepartment: [{ label: 'Operações', value: 10, percentage: 66.67 }],
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as any);

    renderComponent();

    expect(screen.getByText('Dashboard Disciplinar')).toBeDefined();
    expect(screen.getByText('Total de Processos')).toBeDefined();
    expect(screen.getAllByText('15').length).toBeGreaterThan(0);
    expect(screen.getByText('4.5 dias')).toBeDefined();
    expect(screen.getByText('Distribuição por Status')).toBeDefined();
  });
});
