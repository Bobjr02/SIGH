import React from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { formatNumber } from '../../utils/formatters';
import { EmptyState, ErrorState, SkeletonRows } from './States';

export interface Column<T> {
  key: string;
  header: React.ReactNode;
  render: (row: T) => React.ReactNode;
  align?: 'left' | 'right' | 'center';
  /** Esconde a coluna em telas estreitas. */
  hideOnMobile?: boolean;
  className?: string;
}

export interface Paging {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  /** "usuários", "medidas"… */
  noun?: string;
}

interface DataTableProps<T> {
  ariaLabel: string;
  columns: Column<T>[];
  rows?: T[];
  rowKey: (row: T) => string;
  isLoading?: boolean;
  error?: string | null;
  onRetry?: () => void;
  emptyMessage?: string;
  emptyIcon?: React.ReactNode;
  paging?: Paging;
  rowTestId?: string;
  /** data-testid por linha (ex.: employee-row-50090001). */
  rowTestIdOf?: (row: T) => string;
  rowClassName?: (row: T) => string | undefined;
  emptyTestId?: string;
}

/**
 * Tabela no estilo da referência (tabela de usuários): cabeçalho discreto, linhas com hover, estados de
 * carregando (esqueleto), vazio (ícone + mensagem) e erro, e paginação "Mostrando 1–5 de 248".
 */
export function DataTable<T>({
  ariaLabel,
  columns,
  rows,
  rowKey,
  isLoading,
  error,
  onRetry,
  emptyMessage = 'Nenhum registro encontrado.',
  emptyIcon,
  paging,
  rowTestId,
  rowTestIdOf,
  rowClassName,
  emptyTestId,
}: DataTableProps<T>) {
  const align = (a?: Column<T>['align']) => (a === 'right' ? 'text-right' : a === 'center' ? 'text-center' : 'text-left');
  const hide = (c: Column<T>) => (c.hideOnMobile ? 'hidden md:table-cell' : '');

  return (
    <div className="overflow-hidden rounded-lg border border-line bg-surface shadow-card">
      {error ? (
        <ErrorState message={error} onRetry={onRetry} />
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse text-sm" aria-label={ariaLabel} aria-busy={isLoading || undefined}>
            <thead>
              <tr className="bg-surface-2">
                {columns.map((c) => (
                  <th key={c.key} scope="col" className={`whitespace-nowrap px-4 py-3 text-[0.8125rem] font-semibold text-muted ${align(c.align)} ${hide(c)}`}>
                    {c.header}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="text-ink">
              {isLoading && <SkeletonRows columns={columns.length} />}
              {!isLoading && rows?.length === 0 && (
                <tr>
                  <td colSpan={columns.length}>
                    <EmptyState message={emptyMessage} icon={emptyIcon} testId={emptyTestId} />
                  </td>
                </tr>
              )}
              {!isLoading &&
                rows?.map((row) => (
                  <tr key={rowKey(row)} data-testid={rowTestIdOf?.(row) ?? rowTestId} className={`transition-colors hover:bg-surface-2 ${rowClassName?.(row) ?? ''}`}>
                    {columns.map((c) => (
                      <td key={c.key} className={`border-t border-line px-4 py-3 align-middle ${align(c.align)} ${hide(c)} ${c.className ?? ''}`}>
                        {c.render(row)}
                      </td>
                    ))}
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      )}
      {paging && !error && paging.totalCount > 0 && <PaginationBar {...paging} />}
    </div>
  );
}

/** Números de página com reticências: 1 2 3 … 50. */
export const pageItems = (current: number, total: number): (number | '…')[] => {
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1);
  const set = new Set([1, total, current - 1, current, current + 1]);
  if (current <= 3) [2, 3, 4].forEach((p) => set.add(p));
  if (current >= total - 2) [total - 3, total - 2, total - 1].forEach((p) => set.add(p));
  const pages = [...set].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b);
  const result: (number | '…')[] = [];
  pages.forEach((p, i) => {
    if (i > 0 && p - pages[i - 1] > 1) result.push('…');
    result.push(p);
  });
  return result;
};

export const PaginationBar: React.FC<Paging> = ({ pageNumber, pageSize, totalCount, onPageChange, noun = 'registros' }) => {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const from = (pageNumber - 1) * pageSize + 1;
  const to = Math.min(totalCount, pageNumber * pageSize);
  const button = 'inline-flex h-8 min-w-8 items-center justify-center rounded-md px-2 text-sm font-semibold';

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-t border-line px-4 py-3 text-sm text-muted">
      <span data-testid="pagination-summary">
        Mostrando {formatNumber(from)}–{formatNumber(to)} de {formatNumber(totalCount)} {noun}
      </span>
      {totalPages > 1 && (
        <nav aria-label="Paginação" className="flex items-center gap-1">
          <button
            type="button"
            className={`${button} text-ink hover:bg-surface-2 disabled:opacity-40`}
            onClick={() => onPageChange(pageNumber - 1)}
            disabled={pageNumber <= 1}
            aria-label="Página anterior"
          >
            <ChevronLeft size={16} aria-hidden="true" />
          </button>
          {pageItems(pageNumber, totalPages).map((p, i) =>
            p === '…' ? (
              <span key={`gap-${i}`} className="px-1" aria-hidden="true">
                …
              </span>
            ) : (
              <button
                key={p}
                type="button"
                onClick={() => onPageChange(p)}
                aria-label={`Página ${p}`}
                aria-current={p === pageNumber ? 'page' : undefined}
                className={`${button} ${p === pageNumber ? 'bg-primary text-white' : 'text-ink hover:bg-surface-2'}`}
              >
                {p}
              </button>
            ),
          )}
          <button
            type="button"
            className={`${button} text-ink hover:bg-surface-2 disabled:opacity-40`}
            onClick={() => onPageChange(pageNumber + 1)}
            disabled={pageNumber >= totalPages}
            aria-label="Próxima página"
          >
            <ChevronRight size={16} aria-hidden="true" />
          </button>
        </nav>
      )}
    </div>
  );
};
