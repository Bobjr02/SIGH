import React from 'react';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { ArrowLeft, ChevronRight } from 'lucide-react';

export interface Crumb {
  label: string;
  to?: string;
}

interface PageHeaderProps {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  icon?: React.ReactNode;
  actions?: React.ReactNode;
  testId?: string;
}

/** Título da página (como "Usuários / Gerencie os usuários do sistema" da referência). */
export const PageHeader: React.FC<PageHeaderProps> = ({ title, subtitle, icon, actions, testId }) => (
  <div className="mb-5 flex flex-wrap items-start justify-between gap-3" data-testid={testId}>
    <div className="flex items-center gap-3">
      {icon && (
        <span className="inline-flex h-11 w-11 items-center justify-center rounded-lg bg-primary-soft text-primary-text" aria-hidden="true">
          {icon}
        </span>
      )}
      <div>
        <h1 className="text-[1.75rem] font-bold leading-tight tracking-tight text-ink">{title}</h1>
        {subtitle && <p className="mt-0.5 text-sm text-muted">{subtitle}</p>}
      </div>
    </div>
    {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
  </div>
);

/** Breadcrumb + botão "Voltar" das telas de detalhe. */
export const DetailNav: React.FC<{ crumbs: Crumb[]; backTo: string }> = ({ crumbs, backTo }) => {
  const navigate = useNavigate();
  return (
    <div className="mb-4 flex items-center justify-between gap-3">
      <nav aria-label="Trilha de navegação">
        <ol className="flex flex-wrap items-center gap-1.5 text-sm text-muted">
          {crumbs.map((crumb, i) => (
            <li key={`${crumb.label}-${i}`} className="flex items-center gap-1.5">
              {i > 0 && <ChevronRight size={14} aria-hidden="true" />}
              {crumb.to ? (
                <RouterLink to={crumb.to} className="hover:text-ink hover:underline">
                  {crumb.label}
                </RouterLink>
              ) : (
                <span aria-current="page" className="text-ink">
                  {crumb.label}
                </span>
              )}
            </li>
          ))}
        </ol>
      </nav>
      <button
        type="button"
        onClick={() => (window.history.length > 1 ? navigate(-1) : navigate(backTo))}
        className="inline-flex items-center gap-2 rounded-md border border-line-strong px-3 py-1.5 text-sm font-semibold text-ink hover:bg-surface-2"
      >
        <ArrowLeft size={16} aria-hidden="true" /> Voltar
      </button>
    </div>
  );
};
