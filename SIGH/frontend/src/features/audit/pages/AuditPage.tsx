import React, { useState } from 'react';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { Alert, Box, Button, MenuItem, Pagination, Paper, TextField, Typography } from '@mui/material';
import { ScrollText } from 'lucide-react';
import { auditApi, auditKeys, type AuditLogFilters } from '../api/auditApi';
import { AuditEntryCard } from '../components/AuditEntryCard';
import { ACTION_LABELS, ENTITY_LABELS } from '../components/auditFormat';

const PAGE_SIZE = 25;

const toIso = (local: string, endOfMinute = false) => (local ? new Date(local + (endOfMinute ? ':59' : '')).toISOString() : undefined);

/** Consulta geral da trilha de auditoria (Audit.View). */
export const AuditPage: React.FC = () => {
  const [draft, setDraft] = useState({ entityName: '', entityId: '', userId: '', action: '', from: '', to: '' });
  const [filters, setFilters] = useState<AuditLogFilters>({ pageNumber: 1, pageSize: PAGE_SIZE });

  const { data, isLoading, isError, error, isFetching } = useQuery({
    queryKey: auditKeys.search(filters),
    queryFn: () => auditApi.search(filters),
    placeholderData: keepPreviousData,
  });

  const apply = (event: React.FormEvent) => {
    event.preventDefault();
    setFilters({
      entityName: draft.entityName || undefined,
      entityId: draft.entityId.trim() || undefined,
      userId: draft.userId.trim() || undefined,
      action: draft.action || undefined,
      from: toIso(draft.from),
      to: toIso(draft.to, true),
      pageNumber: 1,
      pageSize: PAGE_SIZE,
    });
  };

  const set = (field: keyof typeof draft) => (e: React.ChangeEvent<HTMLInputElement>) => setDraft((d) => ({ ...d, [field]: e.target.value }));

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Typography variant="h4" component="h1" sx={{ fontWeight: 800, display: 'flex', alignItems: 'center', gap: 1 }}>
        <ScrollText size={28} /> Auditoria
      </Typography>

      <Paper
        component="form"
        onSubmit={apply}
        aria-label="Filtros da auditoria"
        elevation={0}
        sx={{ p: 2, border: '1px solid', borderColor: 'divider', display: 'grid', gap: 2, gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' } }}
      >
        <TextField select size="small" label="Entidade" value={draft.entityName} onChange={set('entityName')}>
          <MenuItem value="">Todas</MenuItem>
          {Object.entries(ENTITY_LABELS).map(([value, label]) => (
            <MenuItem key={value} value={value}>
              {label}
            </MenuItem>
          ))}
        </TextField>
        <TextField size="small" label="Id da entidade" value={draft.entityId} onChange={set('entityId')} />
        <TextField size="small" label="Id do usuário" value={draft.userId} onChange={set('userId')} />
        <TextField select size="small" label="Ação" value={draft.action} onChange={set('action')}>
          <MenuItem value="">Todas</MenuItem>
          {Object.entries(ACTION_LABELS).map(([value, label]) => (
            <MenuItem key={value} value={value}>
              {label}
            </MenuItem>
          ))}
        </TextField>
        <TextField size="small" type="datetime-local" label="De" value={draft.from} onChange={set('from')} slotProps={{ inputLabel: { shrink: true } }} />
        <TextField size="small" type="datetime-local" label="Até" value={draft.to} onChange={set('to')} slotProps={{ inputLabel: { shrink: true } }} />
        <Box sx={{ gridColumn: { md: '1 / -1' }, display: 'flex', justifyContent: 'flex-end' }}>
          <Button type="submit" variant="contained" disabled={isFetching}>
            Filtrar
          </Button>
        </Box>
      </Paper>

      {isError && <Alert severity="error">{(error as Error).message}</Alert>}
      {isLoading && <Typography color="text.secondary">Carregando…</Typography>}

      {data && (
        <>
          <Typography variant="body2" color="text.secondary">
            {data.totalCount} registro(s)
          </Typography>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            {data.items.map((log) => (
              <AuditEntryCard key={log.id} log={log} />
            ))}
          </Box>
          {data.totalPages > 1 && (
            <Pagination
              count={data.totalPages}
              page={data.pageNumber}
              onChange={(_, page) => setFilters((f) => ({ ...f, pageNumber: page }))}
              sx={{ alignSelf: 'center' }}
            />
          )}
        </>
      )}
    </Box>
  );
};

export default AuditPage;
