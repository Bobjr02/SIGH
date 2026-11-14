import { describe, expect, it, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/server';
import { API, makeUser, meReturns, ok, refreshSucceeds, renderApp } from '../../../test/auth';
import { PERMISSIONS } from '../../../auth/permissions';
import { historyImportApi, type HistoryPreview } from '../api/historyImportApi';
import { DecisionSupportPanel } from '../../employees/pages/EmployeeDetailPage';
import { RecordsTable } from '../../records/components/RecordsTable';
import { MeasureType, RecordOrigin, RecordStatus } from '../../records/api/recordsApi';
import { MemoryRouter } from 'react-router-dom';

const ADMIN = makeUser({
  id: 'admin-1',
  roles: ['Administrator'],
  permissions: [PERMISSIONS.DISCIPLINARY_HISTORY_IMPORT, PERMISSIONS.USERS_VIEW],
});

const row = (n: number, overrides: Partial<HistoryPreview['toImport'][number]> = {}) => ({
  row: n,
  employeeNumber: '50090007',
  employeeName: 'FRANCISCA PEDRO COSTA SOUZA TESTE',
  date: '2025-03-10',
  measureType: 3,
  measureTypeLabel: 'Advertência escrita',
  infractionName: 'FALTAS INJUSTIFICADAS DE FORMA RECORRENTE',
  newInfractionType: false,
  managementUnitCode: 'USGA141',
  managementUnitName: 'Agrícola Rurícola',
  periodStart: null,
  periodEnd: null,
  suspensionDays: null,
  daysUnknown: false,
  ...overrides,
});

const PREVIEW: HistoryPreview = {
  batchId: 'batch-1',
  fileName: 'base_disciplinar_ficticia.xlsx',
  fileSha256: 'abc',
  expiresAt: '2026-11-14T12:30:00Z',
  summary: { totalRows: 16, toImport: 14, existing: 0, toUpdate: 0, rejected: 2, warnings: 6, newInfractionTypes: 1, absent: 0 },
  toImport: [row(2), row(7, { employeeNumber: '50090000', measureType: 4, measureTypeLabel: 'Suspensão', daysUnknown: true })],
  existing: [],
  toUpdate: [],
  newInfractionTypes: ['USO DE CELULAR EM SERVIÇO'],
  warnings: [{ row: 6, employeeNumber: '50090006', severity: 'Warning', message: 'Nº ADVERTÊNCIA divergente: planilha 1, recalculado 2.' }],
  rejected: [
    { row: 12, employeeNumber: '59990001', severity: 'Error', message: 'Funcionário não encontrado — importe a base completa do SAP antes.' },
    { row: 13, employeeNumber: '50090007', severity: 'Error', message: 'Linha duplicada dentro do arquivo (igual à linha 2).' },
  ],
  absent: [],
  fileWarnings: [],
};

const baseHandlers = (closed = false) => [
  refreshSucceeds(),
  meReturns(ADMIN),
  http.get(`${API}/disciplinary-history/import/status`, () =>
    ok(closed ? { isClosed: true, closedAt: '2026-11-14T10:00:00Z', closedByName: 'Ada Admin' } : { isClosed: false }),
  ),
  http.get(`${API}/disciplinary-history/import/batches`, () => ok({ items: [], pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
];

describe('importação do histórico (/admin/history-import)', () => {
  it('previews with tabs (to import, rejected, new reasons, warnings) and confirms with the same file', async () => {
    server.use(...baseHandlers());
    // jsdom + FormData + interceptador do msw não convivem: o contrato do upload é testado na API.
    const preview = vi.spyOn(historyImportApi, 'preview').mockResolvedValue(PREVIEW);
    const confirm = vi.spyOn(historyImportApi, 'confirm').mockResolvedValue({
      batchId: 'batch-1', summary: PREVIEW.summary, imported: 14, updated: 0, infractionTypesCreated: 1, appliedAt: '2026-11-14T12:00:00Z',
    });

    renderApp('/admin/history-import');
    const user = userEvent.setup();
    const file = new File(['x'], 'base_disciplinar_ficticia.xlsx');
    await user.upload(await screen.findByLabelText('Planilha de penalidades'), file);
    await user.click(screen.getByRole('button', { name: 'Pré-visualizar' }));

    expect(await screen.findByTestId('history-summary')).toHaveTextContent('16 linhas com dados: 14 a importar');
    expect(preview).toHaveBeenCalledWith(file);
    const tabs = screen.getByRole('tablist', { name: 'Pré-visualização do histórico' });
    expect(within(tabs).getByRole('tab', { name: 'A importar (14)' })).toBeInTheDocument();
    expect(screen.getByRole('table', { name: 'A importar' })).toHaveTextContent('não informado');

    await user.click(within(tabs).getByRole('tab', { name: 'Rejeitadas (2)' }));
    expect(screen.getByRole('table', { name: 'Rejeitadas' })).toHaveTextContent('importe a base completa do SAP antes');
    await user.click(within(tabs).getByRole('tab', { name: 'Motivos novos (1)' }));
    expect(screen.getByRole('list', { name: 'Motivos novos' })).toHaveTextContent('USO DE CELULAR EM SERVIÇO');
    await user.click(within(tabs).getByRole('tab', { name: 'Avisos (6)' }));
    expect(screen.getByRole('table', { name: 'Avisos' })).toHaveTextContent('planilha 1, recalculado 2');

    await user.click(screen.getByRole('button', { name: 'Confirmar importação' }));
    expect(await screen.findByText(/14 registro\(s\) novo\(s\)/)).toBeInTheDocument();
    expect(confirm).toHaveBeenCalledWith('batch-1', file);
  });

  it('closing requires an explicit confirmation; afterwards the upload disappears', async () => {
    let closed = false;
    server.use(
      // Antes dos handlers base: na mesma chamada, o primeiro handler da rota ganha.
      http.get(`${API}/disciplinary-history/import/status`, () =>
        ok(closed ? { isClosed: true, closedAt: '2026-11-14T10:00:00Z', closedByName: 'Ada Admin' } : { isClosed: false }),
      ),
      ...baseHandlers(),
      http.post(`${API}/disciplinary-history/import/close`, () => {
        closed = true;
        return ok({ isClosed: true, closedAt: '2026-11-14T10:00:00Z', closedByName: 'Ada Admin' });
      }),
    );

    renderApp('/admin/history-import');
    const user = userEvent.setup();
    await user.click(await screen.findByRole('button', { name: 'Encerrar importação de histórico' }));
    const dialog = await screen.findByRole('dialog', { name: 'Encerrar a importação de histórico?' });
    const button = within(dialog).getByRole('button', { name: 'Encerrar' });
    expect(button).toBeDisabled();
    await user.click(within(dialog).getByRole('checkbox', { name: /definitivo/ }));
    await user.click(button);

    expect(await screen.findByTestId('history-closed')).toHaveTextContent('Ada Admin');
    expect(screen.queryByLabelText('Planilha de penalidades')).not.toBeInTheDocument();
  });

  it('shows the 409 message when the server refuses (import closed)', async () => {
    server.use(...baseHandlers());
    vi.spyOn(historyImportApi, 'preview').mockRejectedValue(new Error('A importação do histórico disciplinar foi encerrada.'));
    renderApp('/admin/history-import');
    const user = userEvent.setup();
    await user.upload(await screen.findByLabelText('Planilha de penalidades'), new File(['x'], 'a.xlsx'));
    await user.click(screen.getByRole('button', { name: 'Pré-visualizar' }));
    expect(await screen.findByRole('alert')).toHaveTextContent('foi encerrada');
  });

  it('is not in the menu nor reachable without Disciplinary.History.Import', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.USERS_VIEW] })), http.get(`${API}/users`, () => HttpResponse.json({})));
    renderApp('/admin/history-import');
    expect(await screen.findByRole('heading', { name: /403/ })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Importar histórico' })).not.toBeInTheDocument();
  });
});

describe('histórico nas telas de consulta', () => {
  it('decision support shows the origins and the suspensions without days', () => {
    render(
      <DecisionSupportPanel
        data={{
          employeeId: 'e', totalMeasures: 4, warnings: 2, verbalWarnings: 0, writtenWarnings: 2, suspensions: 2, suspensionDays: 4,
          suspensionsWithoutPeriod: 1, otherMeasures: 0, byType: [], byInfractionType: [], lastMeasureAt: '2025-06-02T00:00:00Z',
          measuresLast6Months: 1, measuresLast12Months: 3, sources: ['Histórico', 'SIGH'], historicalMeasures: 3, sighMeasures: 1,
          byOrigin: [
            { label: 'Advertências', total: 2, historical: 1, sigh: 1 },
            { label: 'Suspensões', total: 2, historical: 2, sigh: 0 },
          ],
        }}
      />,
    );
    const origins = screen.getByRole('list', { name: 'Medidas por origem' });
    expect(origins).toHaveTextContent('2 advertências — 1 histórica, 1 no SIGH');
    expect(origins).toHaveTextContent('2 suspensões — 2 históricas, 0 no SIGH');
    expect(screen.getByText(/\+1 suspensão sem dias informados/)).toBeInTheDocument();
  });

  it('records list shows the batch seal and "não informado" days', () => {
    render(
      <MemoryRouter>
        <RecordsTable
          isLoading={false}
          data={{
            items: [
              {
                id: 'h1', caseNumber: 'HIST-2025-1', status: RecordStatus.Historical, statusLabel: 'Histórico importado', employeeId: 'e',
                employeeNumber: '50090000', employeeName: 'FUNCIONARIO TESTE', managementUnitCode: 'USGA132', measureType: MeasureType.Suspension,
                measureTypeLabel: 'Suspensão', measureDate: '2025-09-01', registeredAt: '2026-11-14T10:00:00Z', registeredByUserId: 'a',
                origin: RecordOrigin.Historical, importedAt: '2026-11-14T10:00:00', suspensionDaysUnknown: true,
              },
            ],
            pageNumber: 1, pageSize: 20, totalCount: 1, totalPages: 1,
          }}
        />
      </MemoryRouter>,
    );
    expect(screen.getByTestId('record-status')).toHaveTextContent('Histórico importado');
    expect(screen.getByTestId('history-seal')).toHaveTextContent('Histórico importado — lote de 14/11/2026');
    expect(screen.getByText(/dias: não informado/)).toBeInTheDocument();
  });
});
