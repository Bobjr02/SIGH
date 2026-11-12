import { describe, expect, it } from 'vitest';
import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { server } from '../../../test/server';
import { makeUser, meReturns, refreshSucceeds, renderApp } from '../../../test/auth';
import { PERMISSIONS } from '../../../auth/permissions';

// Formato real de GET /api/v1/disciplinary-cases/{id} (GetDisciplinaryCaseByIdResponse).
const caseDetail = {
  id: 'case-1',
  caseNumber: 'PROC-2026-001',
  companyId: 'c1',
  title: 'Uso indevido de crachá',
  description: 'Descrição',
  status: 3,
  priority: 2,
  openedAt: '2026-11-01T10:00:00Z',
  openedByUserId: 'u1',
  occurrences: [],
  employees: [],
  decisions: [],
  measures: [],
  evidences: [
    {
      id: 'ev-1',
      evidenceType: 1,
      title: 'Relatório da catraca',
      hasFile: true,
      originalFileName: 'catraca.pdf',
      contentType: 'application/pdf',
      fileSize: 2048,
      collectedAt: '2026-11-02T10:00:00Z',
      collectedByUserId: 'u1',
      integrityHash: 'a3f1c2d4e5b60718293a4b5c6d7e8f90112233445566778899aabbccddeeff00',
      status: 1,
      fileUploadedByName: 'Maria Gestora',
    },
  ],
};

const caseHandlers = () => [
  http.get('/api/v1/disciplinary-cases/case-1', () => HttpResponse.json({ success: true, data: caseDetail })),
  http.get('/api/v1/infraction-types', () =>
    HttpResponse.json({ success: true, data: { items: [], pageNumber: 1, pageSize: 100, totalCount: 0, totalPages: 0 } }),
  ),
];

describe('case detail page: evidences and history', () => {
  it('shows the evidence list and, with Audit.View, the "Histórico" tab with the case timeline', async () => {
    server.use(
      refreshSucceeds(),
      meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_CASES_VIEW, PERMISSIONS.AUDIT_VIEW] })),
      ...caseHandlers(),
      http.get('/api/v1/disciplinary-cases/case-1/audit-trail', () =>
        HttpResponse.json({
          success: true,
          data: {
            items: [{ id: 'a1', createdAt: '2026-11-12T13:00:00Z', action: 'Created', entity: 'DisciplinaryCase', entityId: 'case-1', userName: 'Maria Gestora' }],
            pageNumber: 1,
            pageSize: 100,
            totalCount: 1,
            totalPages: 1,
          },
        }),
      ),
    );

    renderApp('/disciplinary/cases/case-1');

    const row = within(await screen.findByTestId('evidence-row-ev-1'));
    expect(row.getByText('catraca.pdf')).toBeInTheDocument();
    expect(row.getByText('Maria Gestora')).toBeInTheDocument();
    expect(row.getByRole('button', { name: 'Baixar catraca.pdf' })).toBeInTheDocument();
    // Sem AddEvidence/RemoveEvidence: só leitura.
    expect(screen.queryByRole('button', { name: /registrar evidência/i })).not.toBeInTheDocument();
    expect(row.queryByRole('button', { name: /remover/i })).not.toBeInTheDocument();

    await userEvent.setup().click(screen.getByRole('tab', { name: 'Histórico' }));

    expect(await screen.findByRole('heading', { name: 'Histórico (1)' })).toBeInTheDocument();
    expect(screen.getByText('Criação')).toBeInTheDocument();
    expect(screen.queryByTestId('evidence-row-ev-1')).not.toBeInTheDocument();
  });

  it('without Audit.View there is no "Histórico" tab', async () => {
    server.use(refreshSucceeds(), meReturns(makeUser({ permissions: [PERMISSIONS.DISCIPLINARY_CASES_VIEW] })), ...caseHandlers());

    renderApp('/disciplinary/cases/case-1');

    await screen.findByTestId('evidence-row-ev-1');
    expect(screen.queryByRole('tab', { name: 'Histórico' })).not.toBeInTheDocument();
  });
});
