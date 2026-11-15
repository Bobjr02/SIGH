import React from 'react';
import type { AuditLogDto } from '../api/auditApi';
import { StatusChip, type Tone } from '../../../components/ui/StatusChip';
import { actionLabel, describeChanges, entityLabel, formatDateTime } from './auditFormat';

const actionTone = (action: string): Tone => {
  if (action === 'Created') return 'success';
  if (action === 'Updated') return 'info';
  if (action === 'SoftDeleted' || action === 'Deleted') return 'warning';
  if (action.includes('Failed') || action.includes('Reuse') || action.includes('Locked')) return 'danger';
  return 'neutral';
};

/** Um registro da trilha, legível; os IDs técnicos ficam num detalhe expansível. */
export const AuditEntryCard: React.FC<{ log: AuditLogDto }> = ({ log }) => {
  const all = describeChanges(log);
  const changes = all.filter((c) => !c.technical);
  const technical = all.filter((c) => c.technical);
  return (
    <article className="rounded-lg border border-line bg-surface p-4 shadow-card" data-testid="audit-entry">
      <div className="flex flex-wrap items-center gap-2">
        <StatusChip tone={actionTone(log.action)} label={actionLabel(log.action)} />
        <span className="text-sm font-semibold text-ink">{entityLabel(log.entity)}</span>
        <span className="text-xs text-muted">
          {formatDateTime(log.createdAt)} · {log.userName ?? 'Sistema'}
        </span>
      </div>
      {changes.length > 0 && (
        <ul className="mt-3 flex flex-col gap-1 text-sm">
          {changes.map((c) => (
            <li key={c.key} className="text-ink" data-testid="audit-change">
              <span className="font-semibold">{c.field}:</span>{' '}
              {c.before !== undefined && c.after !== undefined ? (
                <>
                  <span className="text-muted">{c.before}</span> → <span>{c.after}</span>
                </>
              ) : (
                (c.after ?? c.before)
              )}
            </li>
          ))}
        </ul>
      )}
      <details className="mt-3 text-xs text-muted">
        <summary className="cursor-pointer select-none font-medium hover:text-ink">Detalhes técnicos</summary>
        <dl className="mt-2 grid grid-cols-1 gap-x-6 gap-y-1 sm:grid-cols-2">
          <div>
            <dt className="inline font-semibold">Código do registro: </dt>
            <dd className="inline break-all">{log.entityId ?? '—'}</dd>
          </div>
          <div>
            <dt className="inline font-semibold">Código do usuário: </dt>
            <dd className="inline break-all">{log.userId ?? '—'}</dd>
          </div>
          <div>
            <dt className="inline font-semibold">IP: </dt>
            <dd className="inline">{log.ipAddress ?? '—'}</dd>
          </div>
          <div>
            <dt className="inline font-semibold">Correlação: </dt>
            <dd className="inline break-all">{log.correlationId ?? '—'}</dd>
          </div>
          {technical.map((c) => (
            <div key={c.key}>
              <dt className="inline font-semibold">{c.field}: </dt>
              <dd className="inline break-all">{c.before !== undefined && c.after !== undefined ? `${c.before} → ${c.after}` : (c.after ?? c.before)}</dd>
            </div>
          ))}
        </dl>
      </details>
    </article>
  );
};
