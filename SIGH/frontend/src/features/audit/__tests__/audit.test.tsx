import React from 'react';
import { describe, expect, it } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/server';
import { makeUser, meReturns, refreshSucceeds, renderApp } from '../../../test/auth';
import { PERMISSIONS } from '../../../auth/permissions';
import { CaseAuditTimeline } from '../components/CaseAuditTimeline';
import type { AuditLogDto } from '../api/auditApi';

const page = (items: AuditLogDto[]) => ({ success: true, data: { items, pageNumber: 1, pageSize: 25, totalCount: items.length, totalPages: 1 } });

const created: AuditLogDto = {
  id: 'a1',
  createdAt: '2026-11-12T13:00:00Z',
  action: 'Created',
  entity: 'DisciplinaryCase',
  entityId: 'case-1',
  userName: 'Maria Gestora',
  ipAddress: '10.0.0.5',
  newValues: '{"Title":"Processo X","Status":"Draft"}',
};
const updated: AuditLogDto = {
  id: 'a2',
  createdAt: '2026-11-12T13:05:00Z',
  action: 'Updated',
  entity: 'DisciplinaryEvidence',
  entityId: 'ev-1',
  userName: 'Maria Gestora',
  oldValues: '{"OriginalFileName":null}',
  newValues: '{"OriginalFileName":"laudo.pdf"}',
};
const downloaded: AuditLogDto = {
  id: 'a3',
  createdAt: '2026-11-12T13:06:00Z',
  action: 'Security.EvidenceDownloaded',
  entity: 'DisciplinaryEvidence',
  entityId: 'ev-1',
  userName: 'João Auditor',
  newValues: '{"fileName":"laudo.pdf"}',
};

describe('CaseAuditTimeline', () => {
  it('shows the case history with actions, authors and changed fields', async () => {
    let requested = '';
    server.use(
      http.get('/api/v1/disciplinary-cases/case-1/audit-trail', ({ request }) => {
        requested = new URL(request.url).pathname;
        return HttpResponse.json(page([downloaded, updated, created]));
      }),
    );
    render(
      <QueryClientProvider client={new QueryClient({ defaultOptions: { queries: { retry: false } } })}>
        <CaseAuditTimeline caseId="case-1" />
      </QueryClientProvider>,
    );

    const entries = await screen.findAllByTestId('audit-entry');
    expect(requested).toBe('/api/v1/disciplinary-cases/case-1/audit-trail');
    expect(screen.getByRole('heading', { name: 'Histórico (3)' })).toBeInTheDocument();
    expect(within(entries[0]).getByText('Download de evidência')).toBeInTheDocument();
    expect(within(entries[0]).getByText(/João Auditor/)).toBeInTheDocument();
    expect(within(entries[1]).getByText('Alteração')).toBeInTheDocument();
    expect(within(entries[1]).getByText(/∅ → laudo\.pdf/)).toBeInTheDocument();
    expect(within(entries[2]).getByText('Criação')).toBeInTheDocument();
    expect(within(entries[2]).getByText(/IP 10\.0\.0\.5/)).toBeInTheDocument();
  });
});

describe('/audit page', () => {
  it('requires Audit.View: without it shows 403 and hides the menu item', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_CASES_VIEW] })));

    renderApp('/audit');

    expect(await screen.findByRole('heading', { name: /403/ })).toBeInTheDocument();
    expect(within(screen.getByRole('navigation', { name: 'Menu principal' })).queryByRole('button', { name: /auditoria/i })).not.toBeInTheDocument();
  });

  it('lists the logs and sends the chosen filters to the API', async () => {
    const queries: URLSearchParams[] = [];
    server.use(
      refreshSucceeds(),
      meReturns(makeUser({ permissions: [PERMISSIONS.AUDIT_VIEW] })),
      http.get('/api/v1/audit-logs', ({ request }) => {
        queries.push(new URL(request.url).searchParams);
        return HttpResponse.json(page([created]));
      }),
    );

    renderApp('/audit');

    expect(await screen.findByRole('heading', { name: 'Auditoria' })).toBeInTheDocument();
    expect(within(screen.getByRole('navigation', { name: 'Menu principal' })).getByRole('button', { name: /auditoria/i })).toBeInTheDocument();
    expect(await screen.findByText('1 registro(s)')).toBeInTheDocument();
    expect(queries[0].get('pageNumber')).toBe('1');

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Id da entidade'), 'case-1');
    await user.click(screen.getByLabelText('Ação'));
    await user.click(await screen.findByRole('option', { name: 'Remoção lógica' }));
    await user.click(screen.getByRole('button', { name: 'Filtrar' }));

    await waitFor(() => expect(queries.length).toBeGreaterThan(1));
    const last = queries[queries.length - 1];
    expect(last.get('entityId')).toBe('case-1');
    expect(last.get('action')).toBe('SoftDeleted');
    expect(last.has('userId')).toBe(false);
  });
});
