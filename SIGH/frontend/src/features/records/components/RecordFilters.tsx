import React from 'react';
import { Button, MenuItem, TextField } from '@mui/material';
import { FilterX } from 'lucide-react';
import { options as enumOptions } from '../../../i18n/labels';
import type { RecordFilters as Filters, RecordOptions } from '../api/recordsApi';

interface RecordFiltersProps {
  value: Filters;
  options?: RecordOptions;
  onChange: (next: Filters) => void;
  /** Esconde a busca por funcionário (aba do funcionário). */
  hideEmployee?: boolean;
}

const num = (v: string) => (v === '' ? '' : Number(v));

/**
 * Filtros das medidas: período, tipo, motivo, gerência, setor, situação e origem. Gerências e setores
 * vêm de /disciplinary-records/options, já mascarados pelo escopo do usuário no servidor.
 */
export const RecordFilters: React.FC<RecordFiltersProps> = ({ value, options, onChange, hideEmployee }) => {
  const set = (patch: Partial<Filters>) => onChange({ ...value, ...patch, pageNumber: 1 });
  const select = { select: true, size: 'small' as const, fullWidth: true };

  return (
    <form
      role="search"
      aria-label="Filtros das medidas"
      onSubmit={(e) => e.preventDefault()}
      className="mb-4 grid grid-cols-1 gap-3 rounded-lg border border-line bg-surface p-4 shadow-card sm:grid-cols-2 lg:grid-cols-4 xl:grid-cols-5"
    >
      {!hideEmployee && (
        <TextField size="small" fullWidth label="Funcionário (nome ou matrícula)" value={value.employee ?? ''} onChange={(e) => set({ employee: e.target.value })} />
      )}
      <TextField size="small" fullWidth type="date" label="De" value={value.from ?? ''} onChange={(e) => set({ from: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
      <TextField size="small" fullWidth type="date" label="Até" value={value.to ?? ''} onChange={(e) => set({ to: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
      <TextField {...select} label="Medida" value={value.measureType ?? ''} onChange={(e) => set({ measureType: num(e.target.value) as Filters['measureType'] })}>
        <MenuItem value="">Todas</MenuItem>
        {options?.measureTypes.map((t) => (
          <MenuItem key={t.value} value={t.value}>
            {t.label}
          </MenuItem>
        ))}
      </TextField>
      <TextField {...select} label="Motivo" value={value.infractionTypeId ?? ''} onChange={(e) => set({ infractionTypeId: e.target.value })}>
        <MenuItem value="">Todos</MenuItem>
        {options?.infractionTypes.map((t) => (
          <MenuItem key={t.id} value={t.id}>
            {t.name}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        {...select}
        label="Gerência"
        value={value.managementUnitId ?? ''}
        onChange={(e) => set({ managementUnitId: e.target.value, departmentId: '' })}
        slotProps={{ select: { inputProps: { 'data-testid': 'filter-management-unit' } } }}
      >
        <MenuItem value="">Todas</MenuItem>
        {options?.managementUnits.map((m) => (
          <MenuItem key={m.id} value={m.id}>
            {m.code} – {m.name}
          </MenuItem>
        ))}
      </TextField>
      <TextField {...select} label="Setor" value={value.departmentId ?? ''} onChange={(e) => set({ departmentId: e.target.value })}>
        <MenuItem value="">Todos</MenuItem>
        {options?.departments.map((d) => (
          <MenuItem key={d.id} value={d.id}>
            {d.name}
          </MenuItem>
        ))}
      </TextField>
      <TextField {...select} label="Situação" value={value.status ?? ''} onChange={(e) => set({ status: num(e.target.value) as Filters['status'] })}>
        <MenuItem value="">Todas</MenuItem>
        {enumOptions('RecordStatus').map((o) => (
          <MenuItem key={o.value} value={o.value}>
            {o.label}
          </MenuItem>
        ))}
      </TextField>
      <TextField {...select} label="Origem" value={value.origin ?? ''} onChange={(e) => set({ origin: num(e.target.value) as Filters['origin'] })}>
        <MenuItem value="">Todas</MenuItem>
        {enumOptions('RecordOrigin').map((o) => (
          <MenuItem key={o.value} value={o.value}>
            {o.label}
          </MenuItem>
        ))}
      </TextField>
      <div className="flex items-center">
        <Button
          variant="outlined"
          startIcon={<FilterX size={16} />}
          onClick={() => onChange({ pageNumber: 1, pageSize: value.pageSize, mine: value.mine, employeeId: value.employeeId })}
        >
          Limpar filtros
        </Button>
      </div>
    </form>
  );
};
