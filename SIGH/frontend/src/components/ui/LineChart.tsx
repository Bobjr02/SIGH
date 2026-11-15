import React, { useId } from 'react';
import { formatNumber } from '../../utils/formatters';

export interface Point {
  label: string;
  /** Rótulo completo para leitores de tela e dica ("novembro de 2026"). */
  fullLabel?: string;
  value: number;
}

/** Escala "bonita" do eixo Y: 0, 250, 500… */
const niceMax = (max: number) => {
  if (max <= 4) return 4;
  const pow = 10 ** Math.floor(Math.log10(max));
  const step = [1, 2, 2.5, 5, 10].map((m) => m * pow).find((s) => s * 4 >= max) ?? pow * 10;
  return step * 4;
};

/**
 * Gráfico de linha com área (SVG próprio, sem biblioteca): mantém o bundle pequeno e segue os tokens do
 * tema. Inclui tabela oculta com os mesmos dados para leitores de tela.
 */
export const LineChart: React.FC<{ points: Point[]; height?: number; ariaLabel: string; seriesLabel: string }> = ({
  points,
  height = 240,
  ariaLabel,
  seriesLabel,
}) => {
  const id = useId().replace(/:/g, '');
  const width = 640;
  const pad = { top: 16, right: 16, bottom: 28, left: 44 };
  const max = niceMax(Math.max(0, ...points.map((p) => p.value)));
  const innerW = width - pad.left - pad.right;
  const innerH = height - pad.top - pad.bottom;
  const x = (i: number) => pad.left + (points.length <= 1 ? innerW / 2 : (i * innerW) / (points.length - 1));
  const y = (v: number) => pad.top + innerH - (v / max) * innerH;
  const line = points.map((p, i) => `${i === 0 ? 'M' : 'L'}${x(i).toFixed(1)},${y(p.value).toFixed(1)}`).join(' ');
  const area = points.length ? `${line} L${x(points.length - 1)},${pad.top + innerH} L${x(0)},${pad.top + innerH} Z` : '';
  const ticks = [0, 1, 2, 3, 4].map((t) => (max / 4) * t);

  return (
    <figure className="m-0">
      <svg viewBox={`0 0 ${width} ${height}`} className="h-auto w-full" role="img" aria-label={ariaLabel}>
        <defs>
          <linearGradient id={`area-${id}`} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="var(--sigh-chart-area)" />
            <stop offset="1" stopColor="var(--sigh-chart-area)" stopOpacity="0" />
          </linearGradient>
        </defs>
        {ticks.map((t) => (
          <g key={t}>
            <line x1={pad.left} x2={width - pad.right} y1={y(t)} y2={y(t)} stroke="var(--sigh-chart-grid)" strokeDasharray={t === 0 ? undefined : '3 4'} />
            <text x={pad.left - 8} y={y(t) + 4} textAnchor="end" fontSize="11" fill="var(--sigh-text-muted)">
              {formatNumber(t)}
            </text>
          </g>
        ))}
        {area && <path d={area} fill={`url(#area-${id})`} />}
        {line && <path d={line} fill="none" stroke="var(--sigh-chart-line)" strokeWidth="2.5" strokeLinejoin="round" />}
        {points.map((p, i) => (
          <g key={p.label + i}>
            <circle cx={x(i)} cy={y(p.value)} r="4" fill="var(--sigh-surface)" stroke="var(--sigh-chart-line)" strokeWidth="2">
              <title>{`${p.fullLabel ?? p.label}: ${formatNumber(p.value)}`}</title>
            </circle>
            <text x={x(i)} y={height - 8} textAnchor="middle" fontSize="11" fill="var(--sigh-text-muted)">
              {p.label}
            </text>
          </g>
        ))}
      </svg>
      <table className="sr-only">
        <caption>{ariaLabel}</caption>
        <thead>
          <tr>
            <th scope="col">Mês</th>
            <th scope="col">{seriesLabel}</th>
          </tr>
        </thead>
        <tbody>
          {points.map((p, i) => (
            <tr key={p.label + i}>
              <td>{p.fullLabel ?? p.label}</td>
              <td>{formatNumber(p.value)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </figure>
  );
};

/** Barras horizontais de distribuição (rótulo, quantidade e percentual com vírgula). */
export const DistributionBars: React.FC<{
  items: { key: string | number; label: string; count: number; percentage: number; color?: string }[];
  ariaLabel: string;
  emptyMessage?: string;
}> = ({ items, ariaLabel, emptyMessage = 'Nenhum dado para os filtros escolhidos.' }) =>
  items.length === 0 ? (
    <p className="py-6 text-center text-sm text-muted">{emptyMessage}</p>
  ) : (
    <ul aria-label={ariaLabel} className="flex flex-col gap-3.5">
      {items.map((item) => (
        <li key={item.key} data-testid="distribution-item">
          <div className="mb-1 flex items-center justify-between gap-3 text-sm">
            <span className="truncate font-medium text-ink">{item.label}</span>
            <span className="whitespace-nowrap text-muted">
              {formatNumber(item.count)} · <strong className="text-ink">{`${formatNumber(item.percentage)}%`}</strong>
            </span>
          </div>
          <div className="h-2 overflow-hidden rounded-full bg-surface-3" aria-hidden="true">
            <div className="h-2 rounded-full" style={{ width: `${Math.min(100, Math.max(item.percentage, 1.5))}%`, background: item.color ?? 'var(--sigh-chart-line)' }} />
          </div>
        </li>
      ))}
    </ul>
  );
