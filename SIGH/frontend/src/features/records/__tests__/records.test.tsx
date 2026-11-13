import { describe, expect, it } from 'vitest';
import { fireEvent, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/server';
import { API, makeUser, meReturns, ok, refreshSucceeds, renderApp } from '../../../test/auth';
import { PERMISSIONS } from '../../../auth/permissions';
import { MeasureType, RecordStatus, type RecordDetail, type RecordOptions, type RecordSummary } from '../api/recordsApi';

const REGISTRAR = makeUser({
  id: 'u-reg',
  fullName: 'Ana Auxiliar',
  roles: ['HrRegistrar'],
  permissions: [
    PERMISSIONS.DISCIPLINARY_RECORDS_VIEW,
    PERMISSIONS.DISCIPLINARY_RECORDS_VIEW_ALL,
    PERMISSIONS.DISCIPLINARY_RECORDS_REGISTER,
    PERMISSIONS.EMPLOYEES_VIEW,
    PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_EMPLOYEE_HISTORY,
    PERMISSIONS.DISCIPLINARY_CASES_ADD_EVIDENCE,
  ],
});

const APPROVER = makeUser({
  id: 'u-apr',
  fullName: 'Edu Encarregado',
  roles: ['HrApprover'],
  permissions: [
    PERMISSIONS.DISCIPLINARY_RECORDS_VIEW,
    PERMISSIONS.DISCIPLINARY_RECORDS_VIEW_ALL,
    PERMISSIONS.DISCIPLINARY_RECORDS_APPROVE,
    PERMISSIONS.EMPLOYEES_VIEW,
  ],
});

const MANAGER_132 = makeUser({
  id: 'u-g132',
  fullName: 'Gil Gestor',
  roles: ['Manager'],
  permissions: [PERMISSIONS.DISCIPLINARY_RECORDS_VIEW, PERMISSIONS.EMPLOYEES_VIEW],
});

const OPTIONS: RecordOptions = {
  measureTypes: [
    { value: MeasureType.VerbalWarning, label: 'Advertência verbal' },
    { value: MeasureType.WrittenWarning, label: 'Advertência escrita' },
    { value: MeasureType.Suspension, label: 'Suspensão' },
    { value: MeasureType.JustCauseTermination, label: 'Demissão por justa causa' },
  ],
  infractionTypes: [{ id: 'inf-1', code: 'FALTA', name: 'Falta injustificada' }],
  managementUnits: [
    { id: 'mu-131', code: 'USGA131', name: 'Industrial' },
    { id: 'mu-132', code: 'USGA132', name: 'Manutenção' },
  ],
  departments: [],
};

const summary = (overrides: Partial<RecordSummary> = {}): RecordSummary => ({
  id: 'rec-1',
  caseNumber: 'MED-2026-AAAA0001',
  status: RecordStatus.PendingApproval,
  statusLabel: 'Aguardando aprovação',
  employeeId: 'emp-1',
  employeeNumber: '50090006',
  employeeName: 'FUNCIONARIO FICTICIO',
  managementUnitCode: 'USGA131',
  managementUnitName: 'Industrial',
  departmentName: 'MANUTENCAO INDUSTRIAL',
  measureType: MeasureType.WrittenWarning,
  measureTypeLabel: 'Advertência escrita',
  infractionTypeId: 'inf-1',
  infractionTypeName: 'Falta injustificada',
  measureDate: '2026-11-10',
  firstOccurrenceDate: '2026-11-09',
  suspensionDays: null,
  registeredAt: '2026-11-10T12:00:00Z',
  registeredByUserId: 'u-reg',
  registeredByName: 'Ana Auxiliar',
  ...overrides,
});

const detail = (overrides: Partial<RecordDetail> = {}, summaryOverrides: Partial<RecordSummary> = {}): RecordDetail => ({
  summary: summary(summaryOverrides),
  description: 'Faltou ao turno sem justificativa.',
  occurrenceDates: ['2026-11-09'],
  suspensionStart: null,
  suspensionEnd: null,
  managementUnitCodeAtTime: 'USGA131',
  evidences: [],
  history: [],
  actions: { canApprove: false, canReject: false, canCorrect: false, canCancel: false, canAttach: false, canRemoveEvidence: false },
  ...overrides,
});

const optionsHandler = (options: RecordOptions = OPTIONS) => http.get(`${API}/disciplinary-records/options`, () => ok(options));
const pendingCount = (n: number) => http.get(`${API}/disciplinary-records/pending-count`, () => ok(n));
const emptyList = () => http.get(`${API}/disciplinary-records`, () => ok({ items: [], pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 }));

const choose = async (user: ReturnType<typeof userEvent.setup>, label: RegExp, option: RegExp | string) => {
  await user.click(screen.getByRole('combobox', { name: label }));
  await user.click(await screen.findByRole('option', { name: option }));
};

describe('registrar medida', () => {
  it('shows the decision support when choosing the employee and asks for confirmation on a possible duplicate', async () => {
    const posted: Array<Record<string, unknown>> = [];
    server.use(
      refreshSucceeds(),
      meReturns(REGISTRAR),
      optionsHandler(),
      http.get(`${API}/employees`, () =>
        ok({
          items: [{ id: 'emp-1', employeeNumber: '50090006', fullName: 'FUNCIONARIO FICTICIO', cpfMasked: '***', admissionDate: '2020-01-01', status: 2 }],
          pageNumber: 1,
          pageSize: 20,
          totalCount: 1,
          totalPages: 1,
        }),
      ),
      http.get(`${API}/employees/emp-1/decision-support`, () =>
        ok({
          employeeId: 'emp-1',
          totalMeasures: 2,
          warnings: 2,
          verbalWarnings: 1,
          writtenWarnings: 1,
          suspensions: 0,
          suspensionDays: 0,
          suspensionsWithoutPeriod: 0,
          otherMeasures: 0,
          byType: [],
          byInfractionType: [],
          lastMeasureAt: '2026-05-01T00:00:00Z',
          measuresLast6Months: 1,
          measuresLast12Months: 2,
          sources: ['SIGH'],
        }),
      ),
      http.post(`${API}/disciplinary-records`, async ({ request }) => {
        const body = (await request.json()) as Record<string, unknown>;
        posted.push(body);
        if (!body.confirmPossibleDuplicate) {
          return HttpResponse.json(
            {
              success: false,
              errorCode: 'POSSIBLE_DUPLICATE',
              message: 'Possível duplicidade.',
              data: { record: null, possibleDuplicates: [{ id: 'old', caseNumber: 'MED-2026-OLD00001', status: 1, occurrenceDate: '2026-11-09' }] },
            },
            { status: 409 },
          );
        }
        return ok({ record: detail(), possibleDuplicates: [] }, { status: 201 });
      }),
      http.get(`${API}/disciplinary-records/rec-1`, () => ok(detail())),
    );

    renderApp('/medidas/registrar');
    const user = userEvent.setup();

    const employee = await screen.findByRole('combobox', { name: /funcionário/i });
    await user.type(employee, '5009');
    await user.click(await screen.findByRole('option', { name: /50090006/ }, { timeout: 2000 }));

    // Quadro de apoio à decisão ao escolher o funcionário.
    const support = await screen.findByRole('region', { name: 'Apoio à decisão' });
    expect(support).toBeInTheDocument();

    await choose(user, /motivo/i, 'Falta injustificada');
    await choose(user, /tipo de medida/i, 'Advertência escrita');
    fireEvent.change(screen.getByLabelText('Ocorrência 1'), { target: { value: '2026-11-09' } });
    fireEvent.change(screen.getByLabelText(/data da medida/i), { target: { value: '2026-11-10' } });
    await user.type(screen.getByRole('textbox', { name: /descrição/i }), 'Faltou ao turno sem justificativa.');

    await user.click(within(screen.getByRole('form', { name: 'Registrar medida' })).getByRole('button', { name: 'Registrar medida' }));

    const dialog = await screen.findByRole('dialog', { name: 'Possível duplicidade' });
    expect(within(dialog).getByText(/MED-2026-OLD00001/)).toBeInTheDocument();
    expect(posted[0]).toMatchObject({ employeeId: 'emp-1', infractionTypeId: 'inf-1', measureType: MeasureType.WrittenWarning, occurrenceDates: ['2026-11-09'], measureDate: '2026-11-10', confirmPossibleDuplicate: false });

    await user.click(within(dialog).getByRole('button', { name: 'Registrar mesmo assim' }));

    expect(await screen.findByText('Medida registrada. Aguardando aprovação.')).toBeInTheDocument();
    expect(posted[1]).toMatchObject({ confirmPossibleDuplicate: true });
    expect(screen.getByTestId('location')).toHaveTextContent('/medidas/rec-1');
  });

  it('suspension shows the days counting both ends and requires the period', async () => {
    server.use(refreshSucceeds(), meReturns(REGISTRAR), optionsHandler());
    renderApp('/medidas/registrar');
    const user = userEvent.setup();

    await screen.findByRole('combobox', { name: /tipo de medida/i });
    await choose(user, /tipo de medida/i, 'Suspensão');
    fireEvent.change(screen.getByLabelText(/início da suspensão/i), { target: { value: '2026-11-16' } });
    fireEvent.change(screen.getByLabelText(/fim da suspensão/i), { target: { value: '2026-11-18' } });

    expect(screen.getByTestId('suspension-days')).toHaveTextContent('3 dias de suspensão');
  });
});

describe('aprovação e rejeição', () => {
  it('the approver approves a pending record; the pending counter is in the menu', async () => {
    let approved = false;
    server.use(
      refreshSucceeds(),
      meReturns(APPROVER),
      pendingCount(4),
      http.get(`${API}/disciplinary-records/rec-1`, () =>
        ok(approved
          ? detail({}, { status: RecordStatus.Approved, statusLabel: 'Aprovada' })
          : detail({ actions: { canApprove: true, canReject: true, canCorrect: false, canCancel: false, canAttach: false, canRemoveEvidence: false } })),
      ),
      http.post(`${API}/disciplinary-records/rec-1/approve`, () => {
        approved = true;
        return ok(detail({}, { status: RecordStatus.Approved, statusLabel: 'Aprovada' }));
      }),
    );

    renderApp('/medidas/rec-1');
    const user = userEvent.setup();

    const nav = await screen.findByRole('navigation', { name: 'Menu principal' });
    expect(await within(nav).findByText('4')).toBeInTheDocument();
    expect(within(nav).queryByRole('button', { name: /registrar medida/i })).not.toBeInTheDocument();

    await user.click(await screen.findByRole('button', { name: 'Aprovar' }));

    expect(await screen.findByText('Medida aprovada.')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByTestId('record-status')).toHaveTextContent('Aprovada'));
    expect(screen.queryByRole('button', { name: 'Aprovar' })).not.toBeInTheDocument();
  });

  it('who cannot approve does not see the buttons (the server is the real block)', async () => {
    server.use(refreshSucceeds(), meReturns(REGISTRAR), http.get(`${API}/disciplinary-records/rec-1`, () => ok(detail())));
    renderApp('/medidas/rec-1');

    expect(await screen.findByText('Faltou ao turno sem justificativa.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Aprovar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Rejeitar' })).not.toBeInTheDocument();
  });

  it('rejection requires a reason; the registrant corrects and resends', async () => {
    let state: 'pending' | 'rejected' | 'resent' = 'pending';
    let rejectReason = '';
    let corrected: Record<string, unknown> | null = null;
    const rejectedDetail = () =>
      detail(
        {
          lastRejectionReason: 'Data errada.',
          lastRejectedByName: 'Edu Encarregado',
          lastRejectedAt: '2026-11-11T10:00:00Z',
          actions: { canApprove: false, canReject: false, canCorrect: true, canCancel: true, canAttach: false, canRemoveEvidence: false },
        },
        { status: RecordStatus.Rejected, statusLabel: 'Rejeitada' },
      );

    server.use(
      refreshSucceeds(),
      meReturns(APPROVER),
      pendingCount(1),
      optionsHandler(),
      http.get(`${API}/disciplinary-records/rec-1`, () =>
        ok(state === 'pending'
          ? detail({ actions: { canApprove: true, canReject: true, canCorrect: false, canCancel: false, canAttach: false, canRemoveEvidence: false } })
          : state === 'rejected' ? rejectedDetail() : detail()),
      ),
      http.post(`${API}/disciplinary-records/rec-1/reject`, async ({ request }) => {
        rejectReason = ((await request.json()) as { reason: string }).reason;
        state = 'rejected';
        return ok(rejectedDetail());
      }),
      http.put(`${API}/disciplinary-records/rec-1`, async ({ request }) => {
        corrected = (await request.json()) as Record<string, unknown>;
        state = 'resent';
        return ok({ record: detail(), possibleDuplicates: [] });
      }),
    );

    const { unmount } = renderApp('/medidas/rec-1');
    let user = userEvent.setup();

    await user.click(await screen.findByRole('button', { name: 'Rejeitar' }));
    const dialog = await screen.findByRole('dialog', { name: 'Rejeitar medida' });
    expect(within(dialog).getByRole('button', { name: 'Confirmar' })).toBeDisabled();
    await user.type(within(dialog).getByRole('textbox', { name: /motivo da rejeição/i }), 'Data errada.');
    await user.click(within(dialog).getByRole('button', { name: 'Confirmar' }));

    expect(await screen.findByText('Medida rejeitada e devolvida ao registrante.')).toBeInTheDocument();
    expect(rejectReason).toBe('Data errada.');
    unmount();

    // O registrante abre a medida rejeitada, corrige a data e reenvia.
    server.use(meReturns(REGISTRAR));
    renderApp('/medidas/rec-1');
    user = userEvent.setup();
    expect(await screen.findByTestId('rejection-reason')).toHaveTextContent('Data errada.');
    await user.click(screen.getByRole('button', { name: 'Corrigir e reenviar' }));

    const occurrence = await screen.findByLabelText('Ocorrência 1');
    await waitFor(() => expect(occurrence).toHaveValue('2026-11-09'));
    fireEvent.change(occurrence, { target: { value: '2026-11-08' } });
    await user.click(screen.getByRole('button', { name: 'Reenviar para aprovação' }));

    expect(await screen.findByText('Medida corrigida e reenviada para aprovação.')).toBeInTheDocument();
    expect(corrected).toMatchObject({ employeeId: 'emp-1', occurrenceDates: ['2026-11-08'], measureType: MeasureType.WrittenWarning });
  });
});

describe('escopo por gerência', () => {
  it('the manager only sees the management units of the scope in the filter (mask from the server)', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(MANAGER_132),
      optionsHandler({ ...OPTIONS, managementUnits: [{ id: 'mu-132', code: 'USGA132', name: 'Manutenção' }] }),
      emptyList(),
    );

    renderApp('/medidas');
    const user = userEvent.setup();

    await screen.findByRole('heading', { name: 'Medidas' });

    // Gestor só consulta: nem registrar, nem aprovar.
    expect(screen.queryByRole('button', { name: /registrar medida/i })).not.toBeInTheDocument();
    const nav = screen.getByRole('navigation', { name: 'Menu principal' });
    expect(within(nav).queryByRole('button', { name: /aprovações pendentes/i })).not.toBeInTheDocument();

    await user.click(screen.getByRole('combobox', { name: /gerência/i }));
    const listbox = await screen.findByRole('listbox');
    expect(within(listbox).getByRole('option', { name: /USGA132/ })).toBeInTheDocument();
    expect(within(listbox).queryByRole('option', { name: /USGA131/ })).not.toBeInTheDocument();
  });

  it('a record outside the scope answers 404 and the page says it was not found', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(MANAGER_132),
      http.get(`${API}/disciplinary-records/rec-131`, () => HttpResponse.json({ success: false, message: 'Medida não encontrada.' }, { status: 404 })),
    );
    renderApp('/medidas/rec-131');
    expect(await screen.findByTestId('record-not-found')).toHaveTextContent('Medida não encontrada.');
  });

  it('home of the registrant highlights the rejected records', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(REGISTRAR),
      http.get(`${API}/disciplinary-records`, ({ request }) => {
        expect(new URL(request.url).searchParams.get('mine')).toBe('true');
        return ok({ items: [summary({ status: RecordStatus.Rejected, statusLabel: 'Rejeitada' })], pageNumber: 1, pageSize: 10, totalCount: 1, totalPages: 1 });
      }),
    );
    renderApp('/');

    const mine = await screen.findByTestId('home-mine');
    expect(await within(mine).findByTestId('record-status')).toHaveTextContent('Rejeitada');
    expect(screen.queryByTestId('home-pending')).not.toBeInTheDocument();
  });
});
