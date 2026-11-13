import { describe, expect, it, vi } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/server';
import { makeUser, meReturns, refreshSucceeds, renderApp } from '../../../test/auth';
import { PERMISSIONS } from '../../../auth/permissions';
import { api } from '../../../services/api';
import { EmployeeGroup, EmployeeStatus, employeesApi, maskCpf, type ImportPreview } from '../api/employeesApi';

const ok = <T,>(data: T) => HttpResponse.json({ success: true, data });
const page = <T,>(items: T[]) => ok({ items, pageNumber: 1, pageSize: 20, totalCount: items.length, totalPages: 1 });

const ana = {
  id: 'emp-1',
  employeeNumber: '50090001',
  fullName: 'Ana Sintética',
  cpfMasked: '***.982.247-**',
  admissionDate: '2020-02-01',
  status: EmployeeStatus.Active,
  managementUnitName: 'Agrícola Rurícola',
  departmentName: 'Corte',
  group: EmployeeGroup.Field,
};

const filterOptions = () =>
  http.get('/api/v1/employees/filter-options', () =>
    ok({
      managementUnits: [{ id: 'mu-1', code: 'USGA141', name: 'Agrícola Rurícola' }],
      departments: [{ id: 'd-1', code: '60002000', name: 'Corte', managementUnitId: 'mu-1' }],
    }),
  );

describe('maskCpf', () => {
  it('shows only the six middle digits', () => {
    expect(maskCpf('12398224700')).toBe('***.982.247-**');
    expect(maskCpf('123.982.247-00')).toBe('***.982.247-**');
    expect(maskCpf('***.982.247-**')).toBe('***.982.247-**');
    expect(maskCpf('')).toBe('***.***.***-**');
  });
});

describe('/employees', () => {
  it('lists employees with the CPF masked and sends the filters', async () => {
    const queries: URLSearchParams[] = [];
    server.use(
      refreshSucceeds(),
      meReturns(makeUser({ permissions: [PERMISSIONS.EMPLOYEES_VIEW] })),
      filterOptions(),
      http.get('/api/v1/employees', ({ request }) => {
        queries.push(new URL(request.url).searchParams);
        // Mesmo que um CPF completo chegasse por engano, a tela mascara.
        return page([ana, { ...ana, id: 'emp-2', employeeNumber: '50090002', fullName: 'Bruno Sintético', cpfMasked: '52998224725' }]);
      }),
    );

    renderApp('/employees');

    const row = within(await screen.findByTestId('employee-row-50090001'));
    expect(row.getByText('***.982.247-**')).toBeInTheDocument();
    expect(row.getByText('Campo')).toBeInTheDocument();
    const bruno = within(screen.getByTestId('employee-row-50090002'));
    expect(bruno.getByText('***.982.247-**')).toBeInTheDocument();
    expect(screen.queryByText('52998224725')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /importar do sap/i })).not.toBeInTheDocument();

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Nome ou matrícula'), 'Ana');
    await user.click(screen.getByLabelText('Grupo'));
    await user.click(await screen.findByRole('option', { name: 'Campo' }));
    await user.click(screen.getByLabelText('Gerência'));
    await user.click(await screen.findByRole('option', { name: 'USGA141 - Agrícola Rurícola' }));
    await user.click(screen.getByRole('button', { name: 'Filtrar' }));

    await waitFor(() => expect(queries.length).toBeGreaterThan(1));
    const last = queries[queries.length - 1];
    expect(last.get('searchTerm')).toBe('Ana');
    expect(last.get('group')).toBe(String(EmployeeGroup.Field));
    expect(last.get('managementUnitId')).toBe('mu-1');
  });

  it('requires Employees.View', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_CASES_VIEW] })));

    renderApp('/employees');

    expect(await screen.findByRole('heading', { name: /403/ })).toBeInTheDocument();
  });
});

describe('/employees/:id', () => {
  const detailHandlers = () => [
    http.get('/api/v1/employees/emp-1', () => ok({ ...ana, cpf: '12398224700', jobTitleCode: '50001234', jobTitleName: 'Operador', team: 'A' })),
    http.get('/api/v1/employees/emp-1/decision-support', () =>
      ok({
        employeeId: 'emp-1',
        totalMeasures: 3,
        warnings: 2,
        verbalWarnings: 1,
        writtenWarnings: 1,
        suspensions: 1,
        suspensionDays: 3,
        suspensionsWithoutPeriod: 0,
        otherMeasures: 0,
        byType: [
          { label: 'Advertência verbal', count: 1 },
          { label: 'Advertência escrita', count: 1 },
          { label: 'Suspensão', count: 1 },
        ],
        byInfractionType: [{ label: 'Falta não justificada', count: 2 }],
        lastMeasureAt: '2026-10-05T12:00:00Z',
        measuresLast6Months: 2,
        measuresLast12Months: 3,
        sources: ['SIGH'],
      }),
    ),
    http.get('/api/v1/disciplinary-reports/employees/emp-1/history', () =>
      ok({
        employeeId: 'emp-1',
        cases: [
          { caseId: 'c-1', caseNumber: 'PROC-1', title: 'Faltas', status: 'Completed', openedAt: '2026-09-01T10:00:00Z', measuresCount: 1 },
        ],
      }),
    ),
    http.get('/api/v1/disciplinary-reports/measures', ({ request }) => {
      // O relatório envia os filtros em PascalCase (o ASP.NET não diferencia maiúsculas).
      expect(new URL(request.url).searchParams.get('EmployeeId')).toBe('emp-1');
      return page([{ measureId: 'm-1', caseId: 'c-1', caseNumber: 'PROC-1', measureType: 'Suspensão', appliedAt: '2026-10-05T12:00:00Z' }]);
    }),
  ];

  it('shows the full CPF, the decision support box and the disciplinary timeline', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(
        makeUser({
          permissions: [PERMISSIONS.EMPLOYEES_VIEW, PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY, PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES],
        }),
      ),
      ...detailHandlers(),
    );

    renderApp('/employees/emp-1');

    expect(await screen.findByTestId('employee-cpf')).toHaveTextContent('123.982.247-00');
    const box = within(await screen.findByRole('region', { name: 'Apoio à decisão' }));
    expect(box.getByText('Informativo')).toBeInTheDocument();
    expect(box.getByText('1 verbal(is), 1 escrita(s)')).toBeInTheDocument();
    expect(box.getByText('3 dia(s) no total')).toBeInTheDocument();
    expect(box.getByText('05/10/2026')).toBeInTheDocument();
    expect(within(box.getByRole('list', { name: 'Medidas por motivo' })).getByText(/Falta não justificada/)).toBeInTheDocument();

    const timeline = await screen.findAllByTestId('timeline-item');
    expect(timeline).toHaveLength(2);
    expect(timeline[0]).toHaveTextContent('05/10/2026 · Suspensão');
    expect(timeline[1]).toHaveTextContent('Processo PROC-1 - Faltas');
  });

  it('without disciplinary permissions shows only the registration data', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.EMPLOYEES_VIEW] })), ...detailHandlers());

    renderApp('/employees/emp-1');

    expect(await screen.findByTestId('employee-cpf')).toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Apoio à decisão' })).not.toBeInTheDocument();
    expect(screen.queryByRole('region', { name: 'Histórico disciplinar' })).not.toBeInTheDocument();
  });
});

describe('/employees/import', () => {
  const preview: ImportPreview = {
    batchId: 'batch-1',
    fileName: 'atualizacao.xlsx',
    fileSha256: 'a'.repeat(64),
    expiresAt: '2026-11-13T15:00:00Z',
    summary: { totalRows: 6, created: 1, updated: 1, terminated: 0, terminatedByAbsence: 1, reactivated: 1, unchanged: 1, errors: 1, warnings: 1 },
    created: [{ employeeNumber: '50090006', fullName: 'Fábio Sintético', changes: [{ field: 'Situação', oldValue: null, newValue: 'Ativo' }] }],
    updated: [{ employeeNumber: '50090001', fullName: 'Ana Sintética', changes: [{ field: 'Cargo', oldValue: '50001234', newValue: '50009999' }] }],
    toTerminate: [{ employeeNumber: '50090003', fullName: 'Carla Sintética', changes: [{ field: 'Situação', oldValue: 'Afastado', newValue: 'Desligado' }] }],
    reactivated: [{ employeeNumber: '50090004', fullName: 'Davi Sintético', changes: [{ field: 'Situação', oldValue: 'Desligado', newValue: 'Ativo' }] }],
    unchanged: ['50090005'],
    errors: [{ row: 7, employeeNumber: '50090009', severity: 'Error', message: 'Situação "Aposentado" desconhecida.' }],
    warnings: [{ row: 8, employeeNumber: '50090007', severity: 'Warning', message: 'Possível readmissão: o CPF já pertence à matrícula desligada 50090002.' }],
    newManagementUnits: [],
    activeBefore: 4,
    absenceTerminationPercent: 25,
    maxTerminationPercent: 10,
    requiresMassTerminationConfirmation: true,
  };

  const importUser = () =>
    meReturns(makeUser({ permissions: [PERMISSIONS.EMPLOYEES_VIEW, PERMISSIONS.EMPLOYEES_IMPORT] }));
  const batches = (items: unknown[] = []) => http.get('/api/v1/employees/import/batches', () => page(items));
  const xlsx = () => new File([new Uint8Array(10)], 'atualizacao.xlsx', { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

  it('previews in tabs and only confirms the mass termination after the explicit checkbox', async () => {
    vi.spyOn(employeesApi, 'previewImport').mockResolvedValue(preview);
    const confirm = vi.spyOn(employeesApi, 'confirmImport').mockResolvedValue({
      batchId: 'batch-1',
      summary: { ...preview.summary, errors: 1 },
      appliedAt: '2026-11-13T14:00:00Z',
    });
    server.use(refreshSucceeds(), importUser(), batches());
    const user = userEvent.setup();

    renderApp('/employees/import');
    await user.upload(await screen.findByLabelText('Planilha do SAP'), xlsx());
    await user.click(screen.getByRole('button', { name: 'Pré-visualizar' }));

    // Com erros, abre direto na aba de erros.
    expect(await screen.findByRole('tab', { name: 'Erros (1)', selected: true })).toBeInTheDocument();
    expect(screen.getByText('Situação "Aposentado" desconhecida.')).toBeInTheDocument();

    await user.click(screen.getByRole('tab', { name: 'Alterados (1)' }));
    const updated = within(screen.getByRole('table', { name: 'Alterados' }));
    expect(updated.getByText('50009999')).toBeInTheDocument();
    expect(updated.getByText('50001234')).toBeInTheDocument();
    await user.click(screen.getByRole('tab', { name: 'A desligar (1)' }));
    expect(within(screen.getByRole('table', { name: 'A desligar' })).getByText('Carla Sintética')).toBeInTheDocument();
    await user.click(screen.getByRole('tab', { name: 'Reativados (1)' }));
    expect(screen.getByText('Davi Sintético')).toBeInTheDocument();
    await user.click(screen.getByRole('tab', { name: 'Avisos (1)' }));
    expect(screen.getByText(/Possível readmissão/)).toBeInTheDocument();

    const alert = screen.getByRole('alert');
    expect(alert).toHaveTextContent('25% dos 4 ativos');
    const confirmButton = screen.getByRole('button', { name: 'Confirmar importação' });
    expect(confirmButton).toBeDisabled();

    await user.click(screen.getByLabelText('Confirmo o desligamento em massa'));
    await user.click(confirmButton);

    await waitFor(() => expect(confirm).toHaveBeenCalledWith('batch-1', expect.any(File), true));
    expect(await screen.findByText(/Importação aplicada: 1 novo\(s\)/)).toBeInTheDocument();
  });

  it('shows the server error for a file with a missing column', async () => {
    vi.spyOn(employeesApi, 'previewImport').mockRejectedValue(new Error('Coluna(s) obrigatória(s) não encontrada(s) no cabeçalho: "Chave Organização".'));
    server.use(refreshSucceeds(), importUser(), batches());
    const user = userEvent.setup();

    renderApp('/employees/import');
    await user.upload(await screen.findByLabelText('Planilha do SAP'), xlsx());
    await user.click(screen.getByRole('button', { name: 'Pré-visualizar' }));

    expect(await screen.findByText(/"Chave Organização"/)).toBeInTheDocument();
  });

  it('rejects a non-xlsx file on the client', async () => {
    const previewSpy = vi.spyOn(employeesApi, 'previewImport');
    server.use(refreshSucceeds(), importUser(), batches());
    const user = userEvent.setup({ applyAccept: false });

    renderApp('/employees/import');
    await user.upload(await screen.findByLabelText('Planilha do SAP'), new File(['a;b'], 'base.csv', { type: 'text/csv' }));

    expect(await screen.findByText(/formato .xlsx/)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Pré-visualizar' })).toBeDisabled();
    expect(previewSpy).not.toHaveBeenCalled();
  });

  it('lists the previous imports', async () => {
    server.use(
      refreshSucceeds(),
      importUser(),
      batches([
        {
          id: 'b-1',
          fileName: 'carga_inicial.xlsx',
          fileSha256: 'b'.repeat(64),
          status: 'Applied',
          previewedAt: '2026-11-12T10:00:00Z',
          appliedAt: '2026-11-12T10:01:00Z',
          appliedByName: 'Administrador do Sistema',
          summary: { totalRows: 5, created: 5, updated: 0, terminated: 0, terminatedByAbsence: 0, reactivated: 0, unchanged: 0, errors: 0, warnings: 0 },
          massTerminationConfirmed: false,
        },
      ]),
    );

    renderApp('/employees/import');

    const history = within(await screen.findByRole('table', { name: 'Histórico de importações' }));
    expect(await history.findByText('carga_inicial.xlsx')).toBeInTheDocument();
    expect(history.getByText('Aplicada')).toBeInTheDocument();
    expect(history.getByText('5 / 0 / 0 / 0')).toBeInTheDocument();
  });

  it('requires Employees.Import', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.EMPLOYEES_VIEW] })));

    renderApp('/employees/import');

    expect(await screen.findByRole('heading', { name: /403/ })).toBeInTheDocument();
  });

  it('sends the same file and the mass-termination flag as multipart', async () => {
    const post = vi.spyOn(api, 'post').mockResolvedValue({ data: { success: true, data: { batchId: 'batch-1' } } } as never);

    await employeesApi.confirmImport('batch-1', xlsx(), true);

    const [url, form] = post.mock.calls[0];
    expect(url).toBe('/v1/employees/import/batch-1/confirm');
    expect(((form as FormData).get('file') as File).name).toBe('atualizacao.xlsx');
    expect((form as FormData).get('confirmMassTermination')).toBe('true');
  });
});
