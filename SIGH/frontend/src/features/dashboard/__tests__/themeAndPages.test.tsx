import { describe, expect, it } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { http } from 'msw';
import { server } from '../../../test/server';
import { API, EMPTY_OVERVIEW, makeUser, meReturns, ok, refreshFails, refreshSucceeds, renderApp } from '../../../test/auth';
import { PERMISSIONS } from '../../../auth/permissions';
import { THEME_STORAGE_KEY } from '../../../contexts/ThemeContext';
import { pageItems, PaginationBar } from '../../../components/ui/DataTable';
import { variation } from '../../../components/ui/KpiCard';
import { appliedByOf, validityOf } from '../../records/components/RecordsTable';
import { RecordOrigin, RecordStatus, type RecordSummary } from '../../records/api/recordsApi';

const record = (overrides: Partial<RecordSummary>): RecordSummary => ({
  id: 'r',
  caseNumber: 'MED-1',
  status: RecordStatus.Approved,
  statusLabel: 'Aprovada',
  employeeId: 'e',
  employeeNumber: '1',
  employeeName: 'X',
  measureType: 3,
  measureTypeLabel: 'Advertência escrita',
  registeredAt: '2026-11-01T10:00:00Z',
  registeredByUserId: 'u',
  origin: RecordOrigin.Sigh,
  ...overrides,
});

describe('login', () => {
  it('has "Lembrar-me" unchecked, show/hide password, the version footer and no SSO button', async () => {
    server.use(refreshFails());
    renderApp('/login');
    expect(await screen.findByRole('heading', { name: 'Bem-vindo de volta!' }, { timeout: 5000 })).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: 'Lembrar-me' })).not.toBeChecked();
    expect(screen.getByTestId('login-footer')).toHaveTextContent(/^SIGH v\d+\.\d+\.\d+ • Usina Serra Grande$/);
    expect(screen.queryByText(/SSO|Google|Gestão Pública/i)).not.toBeInTheDocument();

    const password = screen.getByLabelText('Senha', { exact: true });
    expect(password).toHaveAttribute('type', 'password');
    await userEvent.setup().click(screen.getByRole('button', { name: 'Mostrar senha' }));
    expect(password).toHaveAttribute('type', 'text');
  });

  it('sends rememberMe = true when checked', async () => {
    let body: unknown;
    server.use(
      refreshFails(),
      http.post(`${API}/auth/login`, async ({ request }) => {
        body = await request.json();
        return ok({ accessToken: 't', expiresInMinutes: 60, mustChangePassword: false });
      }),
      meReturns(makeUser({ permissions: [] }), 't'),
    );
    renderApp('/login');
    const user = userEvent.setup();
    await user.type(await screen.findByLabelText('E-mail'), 'maria@sigh.com.br');
    await user.type(screen.getByLabelText('Senha', { exact: true }), 'x');
    await user.click(screen.getByRole('checkbox', { name: 'Lembrar-me' }));
    await user.click(screen.getByRole('button', { name: /entrar/i }));
    await waitFor(() => expect(body).toEqual({ email: 'maria@sigh.com.br', password: 'x', rememberMe: true }));
  });
});

describe('theme', () => {
  it('is dark by default and the moon button switches to light, saved in localStorage', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_DASHBOARD] })), http.get(`${API}/disciplinary-records/overview`, () => ok(EMPTY_OVERVIEW)));
    renderApp('/');
    await screen.findByTestId('dashboard');
    expect(document.documentElement.dataset.theme).toBe('dark');
    await userEvent.setup().click(screen.getByTestId('theme-toggle'));
    expect(document.documentElement.dataset.theme).toBe('light');
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('light');
    localStorage.clear();
  });
});

describe('painel', () => {
  it('shows the KPIs with the variation and the period options in Portuguese', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_DASHBOARD] })),
      http.get(`${API}/disciplinary-records/overview`, () =>
        ok({
          ...EMPTY_OVERVIEW,
          measures: { current: 1248, previous: 1000 },
          suspensions: { current: 3, previous: 4 },
          suspensionDays: { current: 7, previous: 2 },
          employees: { current: 5, previous: 0 },
          pendingApproval: 2,
          byMonth: [{ year: 2026, month: 11, count: 4 }],
          recentActivities: [
            { kind: 2, recordId: 'r1', caseNumber: 'MED-2026-1', employeeName: 'Ana', measureType: 3, measureTypeLabel: 'x', actorName: 'Encarregado', at: '2026-11-14T17:20:00Z' },
          ],
        }),
      ),
    );
    renderApp('/');
    expect(await screen.findByTestId('kpi-measures-value')).toHaveTextContent('1.248');
    expect(screen.getByTestId('kpi-measures-delta')).toHaveTextContent('+24,8%');
    expect(screen.getByTestId('kpi-suspensions-delta')).toHaveTextContent('-25%');
    expect(screen.getByText('7 dias de suspensão no total')).toBeInTheDocument();
    expect(screen.getByTestId('kpi-employees-delta')).toHaveTextContent('Sem base no período anterior');
    const activity = screen.getByTestId('activity-item');
    expect(activity).toHaveTextContent('Medida aprovada MED-2026-1');
    expect(activity).toHaveTextContent('Advertência escrita · Ana');
    expect(activity).toHaveTextContent('14/11/2026 14:20 – Encarregado');

    await userEvent.setup().click(screen.getByRole('combobox', { name: 'Período' }));
    const options = within(await screen.findByRole('listbox')).getAllByRole('option').map((o) => o.textContent);
    expect(options).toEqual(['Últimos 30 dias', 'Últimos 90 dias', 'Últimos 12 meses', 'Ano atual']);
  });
});

describe('relatórios', () => {
  it('shows the distribution with Portuguese labels and comma percentages', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_REPORTS_VIEW_MEASURES, PERMISSIONS.DISCIPLINARY_RECORDS_VIEW] })),
      http.get(`${API}/disciplinary-records/options`, () => ok({ measureTypes: [], infractionTypes: [], managementUnits: [], departments: [] })),
      http.get(`${API}/disciplinary-records`, () => ok({ items: [], pageNumber: 1, pageSize: 20, totalCount: 0, totalPages: 0 })),
      http.get(`${API}/disciplinary-records/stats`, () =>
        ok({
          total: 3,
          employees: 2,
          suspensionDays: 2,
          averageDaysToApproval: 1.5,
          byStatus: [{ value: 3, label: 'Aprovada', count: 2, percentage: 66.7 }],
          byType: [{ value: 4, label: 'Suspensão', count: 1, percentage: 33.3 }],
        }),
      ),
    );
    renderApp('/relatorios');
    const byStatus = await screen.findByTestId('by-status');
    await waitFor(() => expect(byStatus).toHaveTextContent('Aprovada2 · 66,7%'));
    expect(screen.getByTestId('by-type')).toHaveTextContent('Suspensão1 · 33,3%');
    expect(screen.getByTestId('report-days-to-approval-value')).toHaveTextContent('1,5');
    // Sem "Prioridades" nem "Resolução": o processo em etapas não existe mais aqui.
    expect(screen.queryByText(/prioridade|resolução/i)).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Exportar CSV' })).not.toBeInTheDocument();
  });
});

describe('tabelas', () => {
  it('pagination shows "Mostrando 1–5 de 248" and page numbers with gaps', async () => {
    render(
      <MemoryRouter>
        <PaginationBar pageNumber={1} pageSize={5} totalCount={248} onPageChange={() => undefined} noun="usuários" />
      </MemoryRouter>,
    );
    expect(screen.getByTestId('pagination-summary')).toHaveTextContent('Mostrando 1–5 de 248 usuários');
    expect(pageItems(1, 50)).toEqual([1, 2, 3, 4, '…', 50]);
    expect(pageItems(25, 50)).toEqual([1, '…', 24, 25, 26, '…', 50]);
  });

  it('vigência só na suspensão e "Aplicado por" com o aprovador ou "Histórico importado"', () => {
    expect(validityOf(record({ measureType: 3 }))).toBe('—');
    expect(validityOf(record({ measureType: 4, suspensionStart: '2026-11-02', suspensionEnd: '2026-11-03' }))).toBe('02/11/2026 a 03/11/2026');
    expect(appliedByOf(record({ appliedByName: 'Encarregado' }))).toBe('Encarregado');
    expect(appliedByOf(record({ origin: RecordOrigin.Historical, status: RecordStatus.Historical }))).toBe('Histórico importado');
    expect(appliedByOf(record({ status: RecordStatus.PendingApproval }))).toBe('—');
    expect(variation(110, 100)).toBe(10);
    expect(variation(5, 0)).toBeNull();
  });
});
