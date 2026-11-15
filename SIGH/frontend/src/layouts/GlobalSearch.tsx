import React, { useEffect, useId, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Search } from 'lucide-react';
import { employeesApi } from '../features/employees/api/employeesApi';
import { InitialsAvatar } from '../components/ui/InitialsAvatar';

/** CPF (11 dígitos, com ou sem máscara): a busca global NÃO aceita, para não incentivar a circulação de CPF. */
const looksLikeCpf = (term: string) => /^\d{3}\.?\d{3}\.?\d{3}-?\d{2}$/.test(term.trim());

/**
 * Busca global "Buscar funcionário por nome ou matrícula…": usa GET /employees (o servidor aplica o
 * escopo do usuário) e leva ao detalhe do funcionário. Nunca busca por CPF.
 */
export const GlobalSearch: React.FC = () => {
  const navigate = useNavigate();
  const listId = useId();
  const [term, setTerm] = useState('');
  const [debounced, setDebounced] = useState('');
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);
  const box = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const t = window.setTimeout(() => setDebounced(term.trim()), 250);
    return () => window.clearTimeout(t);
  }, [term]);

  const cpf = looksLikeCpf(debounced);
  const enabled = debounced.length >= 2 && !cpf;
  const { data, isFetching } = useQuery({
    queryKey: ['global-search', debounced],
    queryFn: () => employeesApi.list({ searchTerm: debounced, pageSize: 8 }),
    enabled,
    staleTime: 30_000,
  });
  const results = enabled ? (data?.items ?? []) : [];

  useEffect(() => {
    const close = (e: MouseEvent) => {
      if (box.current && !box.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', close);
    return () => document.removeEventListener('mousedown', close);
  }, []);

  const go = (id: string) => {
    setOpen(false);
    setTerm('');
    navigate(`/employees/${id}`);
  };

  const onKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setOpen(true);
      setActive((a) => Math.min(a + 1, Math.max(results.length - 1, 0)));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setActive((a) => Math.max(a - 1, 0));
    } else if (e.key === 'Enter' && results[active]) {
      e.preventDefault();
      go(results[active].id);
    } else if (e.key === 'Escape') {
      setOpen(false);
    }
  };

  const showPanel = open && debounced.length >= 2;

  return (
    <div ref={box} className="relative w-full max-w-xl" data-testid="global-search">
      <label htmlFor={`${listId}-input`} className="sr-only">
        Buscar funcionário por nome ou matrícula
      </label>
      <Search size={17} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-muted" aria-hidden="true" />
      <input
        id={`${listId}-input`}
        type="search"
        role="combobox"
        aria-expanded={showPanel}
        aria-controls={`${listId}-list`}
        aria-autocomplete="list"
        aria-activedescendant={showPanel && results[active] ? `${listId}-opt-${active}` : undefined}
        autoComplete="off"
        placeholder="Buscar funcionário por nome ou matrícula…"
        value={term}
        onChange={(e) => {
          setTerm(e.target.value);
          setActive(0);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onKeyDown={onKeyDown}
        className="h-10 w-full rounded-full border border-line-strong bg-surface-2 pl-10 pr-4 text-sm text-ink placeholder:text-muted focus:border-primary-text focus:outline-none"
      />
      {showPanel && (
        <div className="absolute left-0 right-0 top-12 z-50 overflow-hidden rounded-lg border border-line bg-surface shadow-[var(--sigh-shadow-popover)]">
          {cpf ? (
            <p className="px-4 py-3 text-sm text-muted" role="status">
              A busca é por nome ou matrícula. CPF não é aceito aqui.
            </p>
          ) : results.length === 0 ? (
            <p className="px-4 py-3 text-sm text-muted" role="status">
              {isFetching ? 'Buscando…' : 'Nenhum funcionário encontrado.'}
            </p>
          ) : (
            <ul id={`${listId}-list`} role="listbox" aria-label="Funcionários encontrados" className="max-h-80 overflow-y-auto py-1">
              {results.map((e, i) => (
                <li
                  key={e.id}
                  id={`${listId}-opt-${i}`}
                  role="option"
                  aria-selected={i === active}
                  onMouseDown={(ev) => ev.preventDefault()}
                  onClick={() => go(e.id)}
                  onMouseEnter={() => setActive(i)}
                  className={`flex cursor-pointer items-center gap-3 px-4 py-2 ${i === active ? 'bg-surface-2' : ''}`}
                >
                  <InitialsAvatar name={e.fullName} size={30} />
                  <span className="min-w-0">
                    <span className="block truncate text-sm font-semibold text-ink">{e.fullName}</span>
                    <span className="block truncate text-xs text-muted">
                      Matrícula {e.employeeNumber}
                      {e.managementUnitCode ? ` · ${e.managementUnitCode}` : ''}
                    </span>
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
};
