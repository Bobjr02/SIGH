import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DisciplinaryMeasuresReportPage } from '../pages/DisciplinaryMeasuresReportPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../hooks/useDisciplinaryReportPermissions', () => ({
  useDisciplinaryReportPermissions: vi.fn(),
}));

vi.mock('../hooks/useDisciplinaryReports', () => ({
  useDisciplinaryMeasuresReport: vi.fn(),
}));

import { useDisciplinaryReportPermissions } from '../hooks/useDisciplinaryReportPermissions';
import { useDisciplinaryMeasuresReport } from '../hooks/useDisciplinaryReports';

describe('DisciplinaryMeasuresReportPage', () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <DisciplinaryMeasuresReportPage />
      </QueryClientProvider>
    );

  it('renders restricted access message when user lacks ViewMeasures permission', () => {
    vi.mocked(useDisciplinaryReportPermissions).mockReturnValue({
      canViewDashboard: false,
      canViewCases: false,
      canViewMeasures: false,
      canViewEmployeeHistory: false,
      canExport: false,
    });

    vi.mocked(useDisciplinaryMeasuresReport).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as any);

    renderComponent();

    expect(screen.getByText('Acesso Restrito')).toBeDefined();
    expect(
      screen.getByText((content) =>
        content.includes('Você não possui permissão para consultar o relatório de medidas')
      )
    ).toBeDefined();
  });

  it('renders measures table rows when permissions are present and data is loaded', () => {
    vi.mocked(useDisciplinaryReportPermissions).mockReturnValue({
      canViewDashboard: true,
      canViewCases: true,
      canViewMeasures: true,
      canViewEmployeeHistory: true,
      canExport: true,
    });

    vi.mocked(useDisciplinaryMeasuresReport).mockReturnValue({
      data: {
        items: [
          {
            measureId: 'm-1',
            caseId: 'c-1',
            caseNumber: 'PROC-2026-001',
            employeeId: 'e-1',
            employeeName: 'João Santos',
            measureType: 'Suspension',
            description: 'Suspensão de 3 dias por reincidência em atrasos',
            appliedAt: '2026-01-12T14:00:00Z',
            effectiveFrom: '2026-01-13',
            effectiveTo: '2026-01-15',
            appliedByEmployeeId: 'resp-1',
            appliedByEmployeeName: 'Carlos Silva',
          },
        ],
        pageNumber: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as any);

    renderComponent();

    expect(screen.getByText('Medidas Disciplinares Aplicadas')).toBeDefined();
    expect(screen.getByText('PROC-2026-001')).toBeDefined();
    expect(screen.getByText('João Santos')).toBeDefined();
    expect(screen.getByText('Suspension')).toBeDefined();
  });
});
