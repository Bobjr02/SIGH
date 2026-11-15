import React from 'react';
import { AlertTriangle, Inbox } from 'lucide-react';

/** Estado vazio: ícone e mensagem. */
export const EmptyState: React.FC<{ message: string; icon?: React.ReactNode; testId?: string }> = ({ message, icon, testId }) => (
  <div className="flex flex-col items-center justify-center gap-2 px-4 py-10 text-center text-muted" data-testid={testId}>
    <span aria-hidden="true">{icon ?? <Inbox size={32} />}</span>
    <p className="text-sm">{message}</p>
  </div>
);

/** Estado de erro, com "Tentar novamente" opcional. */
export const ErrorState: React.FC<{ message: string; onRetry?: () => void }> = ({ message, onRetry }) => (
  <div role="alert" className="flex flex-col items-center justify-center gap-2 px-4 py-10 text-center">
    <AlertTriangle size={32} className="text-danger" aria-hidden="true" />
    <p className="text-sm text-danger">{message}</p>
    {onRetry && (
      <button type="button" onClick={onRetry} className="text-sm font-semibold text-primary-text hover:underline">
        Tentar novamente
      </button>
    )}
  </div>
);

/** Esqueleto de carregamento em linhas. */
export const SkeletonRows: React.FC<{ rows?: number; columns: number }> = ({ rows = 5, columns }) => (
  <>
    {Array.from({ length: rows }, (_, r) => (
      <tr key={r} aria-hidden="true">
        {Array.from({ length: columns }, (__, c) => (
          <td key={c} className="border-b border-line px-4 py-3">
            <span className="block h-3.5 animate-pulse rounded bg-surface-3" style={{ width: `${55 + ((r + c) % 4) * 10}%` }} />
          </td>
        ))}
      </tr>
    ))}
  </>
);
