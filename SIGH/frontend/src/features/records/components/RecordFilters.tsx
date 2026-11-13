import React from 'react';
import { Box, Button, MenuItem, TextField } from '@mui/material';
import { RECORD_STATUS_LABELS, RecordStatus, type RecordFilters as Filters, type RecordOptions } from '../api/recordsApi';

interface RecordFiltersProps {
  value: Filters;
  options?: RecordOptions;
  onChange: (next: Filters) => void;
}

/**
 * Filtros da lista de medidas. Gerências e setores vêm de /disciplinary-records/options, já mascarados
 * pelo escopo do usuário no servidor: o gestor só vê as próprias gerências.
 */
export const RecordFilters: React.FC<RecordFiltersProps> = ({ value, options, onChange }) => {
  const set = (patch: Partial<Filters>) => onChange({ ...value, ...patch, pageNumber: 1 });
  const unitDepartments = options?.departments ?? [];

  return (
    <Box component="form" role="search" aria-label="Filtros das medidas" sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.5, mb: 2 }} onSubmit={(e) => e.preventDefault()}>
      <TextField size="small" label="Funcionário (nome ou matrícula)" value={value.employee ?? ''} onChange={(e) => set({ employee: e.target.value })} sx={{ minWidth: 240 }} />
      <TextField select size="small" label="Situação" value={value.status ?? ''} onChange={(e) => set({ status: e.target.value === '' ? '' : (Number(e.target.value) as RecordStatus) })} sx={{ minWidth: 190 }}>
        <MenuItem value="">Todas</MenuItem>
        {Object.entries(RECORD_STATUS_LABELS).map(([k, label]) => (
          <MenuItem key={k} value={k}>
            {label}
          </MenuItem>
        ))}
      </TextField>
      <TextField select size="small" label="Medida" value={value.measureType ?? ''} onChange={(e) => set({ measureType: e.target.value === '' ? '' : Number(e.target.value) })} sx={{ minWidth: 200 }}>
        <MenuItem value="">Todas</MenuItem>
        {options?.measureTypes.map((t) => (
          <MenuItem key={t.value} value={t.value}>
            {t.label}
          </MenuItem>
        ))}
      </TextField>
      <TextField select size="small" label="Motivo" value={value.infractionTypeId ?? ''} onChange={(e) => set({ infractionTypeId: e.target.value })} sx={{ minWidth: 200 }}>
        <MenuItem value="">Todos</MenuItem>
        {options?.infractionTypes.map((t) => (
          <MenuItem key={t.id} value={t.id}>
            {t.name}
          </MenuItem>
        ))}
      </TextField>
      <TextField
        select
        size="small"
        label="Gerência"
        value={value.managementUnitId ?? ''}
        onChange={(e) => set({ managementUnitId: e.target.value })}
        sx={{ minWidth: 200 }}
        slotProps={{ select: { inputProps: { 'data-testid': 'filter-management-unit' } } }}
      >
        <MenuItem value="">Todas</MenuItem>
        {options?.managementUnits.map((m) => (
          <MenuItem key={m.id} value={m.id}>
            {m.code} – {m.name}
          </MenuItem>
        ))}
      </TextField>
      <TextField select size="small" label="Setor" value={value.departmentId ?? ''} onChange={(e) => set({ departmentId: e.target.value })} sx={{ minWidth: 200 }}>
        <MenuItem value="">Todos</MenuItem>
        {unitDepartments.map((d) => (
          <MenuItem key={d.id} value={d.id}>
            {d.name}
          </MenuItem>
        ))}
      </TextField>
      <TextField size="small" type="date" label="De" value={value.from ?? ''} onChange={(e) => set({ from: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
      <TextField size="small" type="date" label="Até" value={value.to ?? ''} onChange={(e) => set({ to: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} />
      <Button onClick={() => onChange({ pageNumber: 1, pageSize: value.pageSize, mine: value.mine })}>Limpar</Button>
    </Box>
  );
};
