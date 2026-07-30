import React from 'react';
import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { DisciplinaryCasesReportPage } from '../pages/DisciplinaryCasesReportPage';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../hooks/useDisciplinaryReportPermissions', () => ({
  useDisciplinaryReportPermissions: vi.fn(),
}));

vi.mock('../hooks/useDisciplinaryReports', () => ({
  useDisciplinaryCaseReport: vi.fn(),
  useExportCaseReportCsv: vi.fn(),
}));

import { useDisciplinaryReportPermissions } from '../hooks/useDisciplinaryReportPermissions';
import { useDisciplinaryCaseReport, useExportCaseReportCsv } from '../hooks/useDisciplinaryReports';

describe('DisciplinaryCasesReportPage', () => {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });

  const renderComponent = () =>
    render(
      <QueryClientProvider client={queryClient}>
        <DisciplinaryCasesReportPage />
      </QueryClientProvider>
    );

  it('renders restricted access message when user lacks ViewCases permission', () => {
    vi.mocked(useDisciplinaryReportPermissions).mockReturnValue({
      canViewDashboard: false,
      canViewCases: false,
      canViewMeasures: false,
      canViewEmployeeHistory: false,
      canExport: false,
    });

    vi.mocked(useDisciplinaryCaseReport).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as any);

    vi.mocked(useExportCaseReportCsv).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as any);

    renderComponent();

    expect(screen.getByText('Acesso Restrito')).toBeDefined();
    expect(
      screen.getByText((content) =>
        content.includes('Você não possui permissão para consultar o relatório de processos')
      )
    ).toBeDefined();
  });

  it('renders table rows when permissions are granted and data is returned', () => {
    vi.mocked(useDisciplinaryReportPermissions).mockReturnValue({
      canViewDashboard: true,
      canViewCases: true,
      canViewMeasures: true,
      canViewEmployeeHistory: true,
      canExport: true,
    });

    vi.mocked(useDisciplinaryCaseReport).mockReturnValue({
      data: {
        items: [
          {
            caseId: 'case-1',
            caseNumber: 'PROC-2026-001',
            title: 'Insubordinação grave',
            companyName: 'Empresa Matriz',
            departmentName: 'Operações',
            status: 'Opened',
            priority: 'High',
            responsibleEmployeeName: 'Carlos Silva',
            mainAccusedEmployeeName: 'João Santos',
            openedAt: '2026-01-10T10:00:00Z',
            concludedAt: null,
            resolutionTimeInDays: null,
            occurrencesCount: 1,
            evidencesCount: 2,
            decisionsCount: 0,
            measuresCount: 1,
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

    vi.mocked(useExportCaseReportCsv).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as any);

    renderComponent();

    expect(screen.getByText('Consulta de Processos Disciplinares')).toBeDefined();
    expect(screen.getByText('PROC-2026-001')).toBeDefined();
    expect(screen.getByText('Insubordinação grave')).toBeDefined();
    expect(screen.getByText('João Santos')).toBeDefined();
  });
});
