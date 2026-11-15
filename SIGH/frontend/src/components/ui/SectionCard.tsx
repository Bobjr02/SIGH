import React from 'react';

/** Cartão de seção (superfície com borda, título e ações opcionais). */
export const SectionCard: React.FC<{
  title?: React.ReactNode;
  actions?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
  ariaLabel?: string;
  testId?: string;
}> = ({ title, actions, children, className = '', ariaLabel, testId }) => (
  <section
    aria-label={ariaLabel ?? (typeof title === 'string' ? title : undefined)}
    data-testid={testId}
    className={`rounded-lg border border-line bg-surface p-5 shadow-card ${className}`}
  >
    {(title || actions) && (
      <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
        {title && <h2 className="text-base font-semibold text-ink">{title}</h2>}
        {actions}
      </div>
    )}
    {children}
  </section>
);

/** Campo "rótulo / valor" das telas de detalhe. */
export const Field: React.FC<{ label: string; value?: React.ReactNode; className?: string }> = ({ label, value, className = '' }) => (
  <div className={className}>
    <dt className="text-xs font-medium text-muted">{label}</dt>
    <dd className="mt-0.5 text-sm font-semibold text-ink">{value === null || value === undefined || value === '' ? '—' : value}</dd>
  </div>
);
