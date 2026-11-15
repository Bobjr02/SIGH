import React from 'react';
import { ArrowDown, ArrowUp } from 'lucide-react';
import { formatNumber, formatPercent } from '../../utils/formatters';
import type { Tone } from './StatusChip';

const ICON_TONE: Record<Tone | 'primary', string> = {
  primary: 'bg-primary-soft text-primary-text',
  success: 'bg-success-soft text-success',
  warning: 'bg-warning-soft text-warning',
  danger: 'bg-danger-soft text-danger',
  info: 'bg-info-soft text-info',
  neutral: 'bg-surface-3 text-muted',
};

/** Variação percentual em relação ao período anterior (sem base: sem percentual). */
export const variation = (current: number, previous: number): number | null =>
  previous === 0 ? null : Math.round(((current - previous) / previous) * 1000) / 10;

interface KpiCardProps {
  icon: React.ReactNode;
  tone?: Tone | 'primary';
  value: number;
  label: string;
  /** Valor no período anterior: mostra ↑/↓ com a variação. */
  previous?: number;
  hint?: React.ReactNode;
  testId?: string;
}

/**
 * Cartão de indicador como na referência: ícone, número grande e variação. ↑ verde / ↓ vermelho, como
 * pedido (aqui "subir" é apenas "mais medidas que no período anterior", não um juízo de valor).
 */
export const KpiCard: React.FC<KpiCardProps> = ({ icon, tone = 'primary', value, label, previous, hint, testId }) => {
  const delta = previous === undefined ? undefined : variation(value, previous);
  return (
    <div className="flex flex-col gap-3 rounded-lg border border-line bg-surface p-5 shadow-card" data-testid={testId}>
      <span className={`inline-flex h-10 w-10 items-center justify-center rounded-lg ${ICON_TONE[tone]}`} aria-hidden="true">
        {icon}
      </span>
      <div>
        <p className="text-[1.75rem] font-bold leading-none text-ink" data-testid={testId ? `${testId}-value` : undefined}>
          {formatNumber(value)}
        </p>
        <p className="mt-1.5 text-sm text-muted">{label}</p>
      </div>
      {previous !== undefined && (
        <p className="text-xs font-semibold" data-testid={testId ? `${testId}-delta` : undefined}>
          {delta === null ? (
            <span className="text-muted">{previous === value ? 'Sem variação' : 'Sem base no período anterior'}</span>
          ) : delta >= 0 ? (
            <span className="inline-flex items-center gap-1 text-success">
              <ArrowUp size={13} aria-hidden="true" /> +{formatPercent(delta)}
              <span className="sr-only"> em relação ao período anterior</span>
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 text-danger">
              <ArrowDown size={13} aria-hidden="true" /> {formatPercent(delta)}
              <span className="sr-only"> em relação ao período anterior</span>
            </span>
          )}
        </p>
      )}
      {hint && <p className="text-xs text-muted">{hint}</p>}
    </div>
  );
};
